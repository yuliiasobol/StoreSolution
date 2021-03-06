using System;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyVersion("0.1.0")]
[assembly: AssemblyFileVersion("0.1.0.0")]
[assembly: AssemblyTitle("nunit-color-console")]
[assembly: AssemblyProduct("nunit-color-console")]
[assembly: AssemblyDescription("Tiny wrapper around nunit-console.exe that adds colors")]
[assembly: AssemblyCompany("remi Taylor")]
[assembly: AssemblyCopyright("Copyright © 2011")]

public class NUnitColorConsole {

    static bool     allTestsPassed       = false;
    static bool     summaryHasPrinted    = false;
    static int      msToWaitAfterSummary = 2000;
    static DateTime lastLinePrintedAt    = DateTime.MinValue;
    static Process  process              = null;
    static Thread   outputProcessor      = null;

    // NUnit 2.4.7 displays the summary on 1 line
    static string SummaryLine  = @"^Tests run: (\d+), Failures: (\d+), Not run: (\d+), Time: .* seconds$";

    // NUnit 2.5.8 displays the summary on 2 lines
    static string SummaryLine1 = @"^Tests run: (\d+), Errors: (\d+), Failures: (\d+), Inconclusive: \d+, Time: .* seconds$";
    static string SummaryLine2 = @"^  Not run: \d+, Invalid: \d+, Ignored: (\d+), Skipped: \d+$";

    public static void Main(string[] args) {
        StartProcess(args);
        StartThread();
        WaitForExit();
    }

    static void StartThread() {
        outputProcessor = new Thread(ProcessOutput);
        outputProcessor.Start();
    }

    // Wait for the background Thread to either:
    //
    //  * finish processing output
    //  * or wait for some duration after the summary has been printed, then exit
    //
    // Sometimes when we Read() the process's STDOUT, it blocks for ever so 
    // we put this in a background thread and only wait for a certain amount of time 
    // after the summary has printed.
    //
    static void WaitForExit() {
        while (outputProcessor.IsAlive && ! summaryHasPrinted)
            Thread.Sleep(100); // hang out and wait for the summary to print (or the thread to finish)

        while (summaryHasPrinted && outputProcessor.IsAlive && MsSinceLastLinePrinted < msToWaitAfterSummary)
            Thread.Sleep(100); // hang out and wait for X milliseconds to have passed since the last time nunit-console output a line

        // Everything should be done!  Let's tell the Thread that we're aborting it and give it 1 second to finish up ... then exit!
        if (outputProcessor.IsAlive) {
            outputProcessor.Abort();
            Thread.Sleep(1000);
        }

        // Exit with non-0 exit code unless all of the tests passed
        if (allTestsPassed)
            Environment.Exit(0);
        else
            Environment.Exit(1);
    }

    static long MsSinceLastLinePrinted { get { return (DateTime.Now.Subtract(lastLinePrintedAt).Ticks / 10000); }}

    static string GetNunitConsoleCommand() {
        if (Environment.GetEnvironmentVariable("NUNIT_CONSOLE") != null)
            return Environment.GetEnvironmentVariable("NUNIT_CONSOLE");
        else
            return "nunit-console";
    }

    static void StartProcess(string[] args) {
        process = new System.Diagnostics.Process();
        process.StartInfo.FileName               = GetNunitConsoleCommand();
        process.StartInfo.Arguments              = String.Join(" ", args);
        process.StartInfo.UseShellExecute        = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow         = true;
        process.StartInfo.WorkingDirectory       = System.IO.Directory.GetCurrentDirectory();
        process.Start();
    }

    static void ProcessOutput() {
        bool testCaseFailures = false;
        bool testsNotRun      = false;
        String output         = null;
        Match  match          = null;
        String summary        = null;
        while ((output = process.StandardOutput.ReadLine()) != null) {
            match             = null;
            lastLinePrintedAt = DateTime.Now;

            // We're printing out the summary line.  Color it based on if everything passed, there were failures, or nothing ran
            //
            //   Tests run: 7, Failures: 1, Not run: 8, Time: 0.411 seconds
            //
            if ((match = Regex.Match(output, SummaryLine)).Success) {
                var run     = int.Parse(match.Groups[1].Value);
                var fails   = int.Parse(match.Groups[2].Value);
                var pending = int.Parse(match.Groups[3].Value);

                if      (fails > 0) Console.ForegroundColor = ConsoleColor.Red;
                else if (run > 0)   Console.ForegroundColor = ConsoleColor.Green;
                else                Console.ForegroundColor = ConsoleColor.Yellow;

                if (Console.ForegroundColor == ConsoleColor.Green) allTestsPassed = true;

                Console.WriteLine(output);
                Console.ForegroundColor = ConsoleColor.White;
                summaryHasPrinted       = true;

            // Some versions of NUnit console print out the summary on 2 lines, using different verbiage
            //  
            //   Tests run: 7, Errors: 0, Failures: 1, Inconclusive: 0, Time: 0.2217072 seconds
            //     Not run: 8, Invalid: 0, Ignored: 8, Skipped: 0
            //   
            } else if ((match = Regex.Match(output, SummaryLine1)).Success) {
                summary = output; // we can't print out the results until we read the second line of the summary

            // The second line of the summary (in some versions of NUnit console)
            } else if ((match = Regex.Match(output, SummaryLine2)).Success) {
                var summaryMatch = Regex.Match(summary, SummaryLine1);

                var run     = int.Parse(summaryMatch.Groups[1].Value);
                var errors  = int.Parse(summaryMatch.Groups[2].Value);
                var fails   = int.Parse(summaryMatch.Groups[3].Value);
                var pending = int.Parse(match.Groups[1].Value);

                if      (errors > 0 || fails > 0) Console.ForegroundColor = ConsoleColor.Red;
                else if (run > 0)                 Console.ForegroundColor = ConsoleColor.Green;
                else                              Console.ForegroundColor = ConsoleColor.Yellow;

                if (Console.ForegroundColor == ConsoleColor.Green) allTestsPassed = true;

                Console.WriteLine(summary);
                Console.WriteLine(output);
                Console.ForegroundColor = ConsoleColor.White;
                summaryHasPrinted       = true;

            // The following lines are all going to be the tests that were not run, so set testsNotRun = true
            } else if (output == "Tests not run:" || output == "Tests Not Run:") {
                Console.WriteLine(output);
                testsNotRun = true;

            // The following lines are all going to be the test case failures
            } else if (output == "Test Case Failures:" || output == "Errors and Failures:") {
                Console.WriteLine(output);
                testCaseFailures = true;

            // We're printing out the section of [Ignore] tests (tests not run)
            } else if (testsNotRun) {
                match = Regex.Match(output, @"^(\d+)\) ([^:]+) : (.*)$");
                if (match.Success) {
                    var testNumber   = match.Groups[1].Value;
                    var testName     = match.Groups[2].Value;
                    var ignoreReason = match.Groups[3].Value;

                    // on some versions of NUnit console, it displays 1) Ignored : TestName
                    if (testName == "Ignored") {
                        testName     = ignoreReason;
                        ignoreReason = "";
                    }

                    // Set a default [Ignore] reason if the user didn't specify one
                    if (ignoreReason.Trim().Length == 0)
                        ignoreReason = "Pending (Not Yet Implemented)";

                    Console.Write("{0}) ", testNumber);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(testName);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(" : {0}", ignoreReason);
                }

            // We're printing out the section of failures
            } else if (testCaseFailures) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(output);
                Console.ForegroundColor = ConsoleColor.White;

            // None of the conditions hit ... simply print out the line!
            } else {
                Console.WriteLine(output);
            }
        }
    }
}

﻿using System;
using System.Drawing;
using Ranorex.Core.Testing;
using Applitools;
using Applitools.ImageTester;

namespace Ranorex.Eyes
{
    internal static class EyesWrapper
    {
        internal static readonly log4net.LogManager mgr = null; // explicit ref

        private static readonly Applitools.Images.Eyes eyes = new Applitools.Images.Eyes();
        private static readonly BatchInfo batch = new BatchInfo();

        public static string CurrentTestName { get; set; }
        public static bool TestRunning { get; set; }
        public static int ViewPortHeight { get; set; }
        public static int ViewPortWidth { get; set; }
        public static string ApiKey { get; set; }
        public static string ServerURL { get; set; }

        public static void Initialize(
            string apiKey,
            string serverURL,
            string batchId,
            int portWidth,
            int portHeight,
            string browserName,
            string matchLevel)
        {
            eyes.SetAppEnvironment(Host.Local.OSEdition, browserName);
            eyes.ApiKey = apiKey;

            ApiKey = apiKey;
            if (!string.IsNullOrWhiteSpace(serverURL))
            {
                eyes.ServerUrl = serverURL;
                ServerURL = serverURL;
            }

            SetMatchLevel(matchLevel);

            SetBatch(TestSuite.Current.Name, batchId);

            ViewPortWidth = portWidth;
            ViewPortHeight = portHeight;
            CurrentTestName = string.Empty;
        }

        public static void CheckImage(Bitmap image, string tag)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            eyes.CheckImage(image, tag);
        }

        public static void CheckFolder(string fileOrFolderPath, string appName)
        {
            var viewPort = Size.Empty;
            if (ViewPortWidth > 0 && ViewPortHeight > 0)
            {
                viewPort = new Size(ViewPortWidth, ViewPortHeight);
            }

            var builder = new SuiteBuilder(fileOrFolderPath, appName, viewPort);

            var suite = builder.Build();
            if (suite == null)
            {
                Console.WriteLine("Nothing to test!");
                return;
            }

            Report.Info("Visual Checkpoint - file comparison (PDF/Images).");

            suite.Run(eyes);
        }

        public static void CloseTest(bool throwException)
        {
            if (TestRunning)
            {
                try
                {
                    eyes.Close(throwException);
                }
                catch (NewTestException e)
                {
                    Report.LogHtml(ReportLevel.Warn, "Visual Testing", string.Format("New baseline created; please approve here: <a href='{0}'>Applitools backend</a>", e.TestResults.Url));
                }
                catch (TestFailedException e)
                {
                    Report.LogHtml(ReportLevel.Failure, "Visual Testing", string.Format("Visual test failed; please check results here: <a href='{0}'>Applitools backend</a>", e.TestResults.Url));
                }
                finally
                {
                    TestRunning = false;
                }
            }
        }

        public static void StartOrContinueTest(string appName, string testName)
        {
            if (!TestRunning)
            {
                eyes.Open(appName, testName, new Size(ViewPortWidth, ViewPortHeight));
                CurrentTestName = testName;
                TestRunning = true;
            }
            else
            {
                if (!testName.Equals(CurrentTestName))
                {
                    CloseTest(true);
                    StartOrContinueTest(appName, testName);
                }
            }
        }

        public static void SetMatchLevel(string matchLevel)
        {
            Report.Debug(String.Format("Setting Match-Level to: {0}", matchLevel));

            if (!string.IsNullOrEmpty(matchLevel))
            {
                if (matchLevel.ToUpper().Contains("LAYOUT"))
                {
                    eyes.DefaultMatchSettings.MatchLevel = MatchLevel.Layout;
                }
                else if (matchLevel.ToUpper().Contains("EXACT"))
                {
                    eyes.DefaultMatchSettings.MatchLevel = MatchLevel.Exact;
                }
                else if (matchLevel.ToUpper().Contains("CONTENT"))
                {
                    eyes.DefaultMatchSettings.MatchLevel = MatchLevel.Content;
                }
                else
                {
                    Report.Warn(string.Format("MatchLevel {0} is not valid; fallback to default (strict)", matchLevel));
                    eyes.DefaultMatchSettings.MatchLevel = MatchLevel.Strict; // Default
                }
            }
            else
            {
                eyes.DefaultMatchSettings.MatchLevel = MatchLevel.Strict; // Default
            }
        }

        public static void SetBatch(string batchName, string batchId)
        {
            batch.Id = batchId;
            SetBatch(batchName);
        }

        public static void SetBatch(string batchName)
        {
            batch.Name = batchName;
            eyes.Batch = batch;
        }
    }
}
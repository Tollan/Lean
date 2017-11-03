/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
using System;
using System.Globalization;
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.GDAXDownloader
{
    class Program
    {
        /// <summary>
        /// GDAX Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                args = new [] { args[0], DateTime.UtcNow.ToString("yyyyMMdd"), args[1] };
            }
            else if (args.Length < 3)
            {
                Console.WriteLine("Usage: GDAX Downloader SYMBOL FROMDATE TODATE");
                Console.WriteLine("FROMDATE = yyyymmdd");
                Console.WriteLine("TODATE = yyyymmdd");
                Environment.Exit(1);
            }

            try
            {
                // Load settings from command line
                var startDate = DateTime.ParseExact(args[1], "yyyyMMdd", CultureInfo.InvariantCulture);
                var endDate = DateTime.ParseExact(args[2], "yyyyMMdd", CultureInfo.InvariantCulture);

                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");
                //todo: will download any exchange but always save as gdax
                // Create an instance of the downloader
                const string market = Market.GDAX;
                var downloader = new GDAXDownloader();

                // Download the data
                var symbolObject = Symbol.Create(args[0], SecurityType.Crypto, market);
                var data = downloader.Get(symbolObject, Resolution.Hour, startDate, endDate);

                // Save the data
                
                var writer = new LeanDataWriter(Resolution.Hour, symbolObject, dataDirectory, TickType.Trade);
                writer.Write(data);
                
                Console.WriteLine("Finish data download");
                
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            Console.ReadLine();
        }
    }
}

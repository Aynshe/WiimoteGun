using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace WiimoteGun
{
    /// <summary>
    /// Provides lookup for USB Vendor and Product names using usb.ids database
    /// (EN/FR: Fournit la recherche de noms USB Vendeur/Produit via base de données usb.ids)
    /// </summary>
    public static class UsbIdProvider
    {
        private static Dictionary<string, string> _vendors = new Dictionary<string, string>();
        private static Dictionary<string, Dictionary<string, string>> _products = new Dictionary<string, Dictionary<string, string>>();
        private static bool _isInitialized = false;
        private static string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "usb.ids");

        public static void Initialize()
        {
            if (_isInitialized) return;

            if (!File.Exists(_dbPath))
            {
                DownloadDatabase();
            }

            if (File.Exists(_dbPath))
            {
                ParseDatabase();
            }
            
            _isInitialized = true;
        }

        private static void DownloadDatabase()
        {
            string[] urls = new[] 
            {
                "https://raw.githubusercontent.com/usbids/usbids/master/usb.ids", // Reliable GitHub mirror (HTTPS)
                "http://www.linux-usb.org/usb.ids" // Official source (HTTP, sometimes unstable)
            };

            foreach (string url in urls)
            {
                try
                {
                    SimpleLogger.Instance.Info($"Downloading usb.ids database from {url}...");
                    using (var client = new WebClient())
                    {
                        // Set a user agent to avoid being blocked
                        client.Headers.Add("User-Agent", "WiimoteGun/1.0");
                        client.DownloadFile(url, _dbPath);
                    }
                    
                    if (File.Exists(_dbPath) && new FileInfo(_dbPath).Length > 0)
                    {
                        SimpleLogger.Instance.Info("usb.ids database downloaded successfully.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Warning($"Failed to download from {url}: {ex.Message}");
                }
            }
            
            SimpleLogger.Instance.Error("All attempts to download usb.ids failed.");
        }

        private static void ParseDatabase()
        {
            try
            {
                string[] lines = File.ReadAllLines(_dbPath);
                string currentVendor = null;

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    // Vendor line: "046d  Logitech, Inc." (no indentation)
                    if (!line.StartsWith("\t"))
                    {
                        var match = Regex.Match(line, @"^([0-9a-fA-F]{4})\s+(.+)$");
                        if (match.Success)
                        {
                            currentVendor = match.Groups[1].Value.ToLower();
                            string vendorName = match.Groups[2].Value.Trim();
                            _vendors[currentVendor] = vendorName;
                            _products[currentVendor] = new Dictionary<string, string>();
                        }
                    }
                    // Product line: "\tC077  Mouse M105" (one tab indentation)
                    else if (currentVendor != null && line.StartsWith("\t") && !line.StartsWith("\t\t"))
                    {
                        var match = Regex.Match(line, @"^\t([0-9a-fA-F]{4})\s+(.+)$");
                        if (match.Success)
                        {
                            string productId = match.Groups[1].Value.ToLower();
                            string productName = match.Groups[2].Value.Trim();
                            _products[currentVendor][productId] = productName;
                        }
                    }
                }
                SimpleLogger.Instance.Info($"Parsed usb.ids: {_vendors.Count} vendors loaded.");
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to parse usb.ids: {ex.Message}");
            }
        }

        public static string GetVendorName(string vid)
        {
            Initialize();
            if (string.IsNullOrEmpty(vid)) return null;
            vid = vid.ToLower();
            return _vendors.ContainsKey(vid) ? _vendors[vid] : null;
        }

        public static string GetProductName(string vid, string pid)
        {
            Initialize();
            if (string.IsNullOrEmpty(vid) || string.IsNullOrEmpty(pid)) return null;
            vid = vid.ToLower();
            pid = pid.ToLower();

            if (_products.ContainsKey(vid) && _products[vid].ContainsKey(pid))
            {
                return _products[vid][pid];
            }
            return null;
        }
    }
}

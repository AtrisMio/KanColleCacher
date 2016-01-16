using Codeplex.Data;
using d_f_32.KanColleCacher;
using d_f_32.KanColleCacher.Configuration;
using Fiddler;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace Gizeta.KanColleCacher
{
    public class ImageModifier
    {
        private Settings set = Settings.Current;
        private string jsonData = "";

        public ImageModifier()
        {
            this.Initialize();
        }

        public void Initialize()
        {
            FiddlerApplication.BeforeRequest += Image_BeforeRequest;
            FiddlerApplication.BeforeResponse += Image_BeforeResponse;
        }

        public void Dispose()
        {
            FiddlerApplication.BeforeRequest -= Image_BeforeRequest;
            FiddlerApplication.BeforeResponse -= Image_BeforeResponse;
            jsonData = "";
        }
        private void Image_BeforeRequest(Session oSession)
        {
            if (!set.CacheEnabled) return;

            if (oSession.PathAndQuery.StartsWith("/kcsapi/api_start2"))
            {
                oSession.bBufferResponse = true;
            }
        }

        private void Image_BeforeResponse(Session oSession)
        {
            if (!set.CacheEnabled) return;

            if (oSession.PathAndQuery.StartsWith("/kcsapi/api_start2") && Settings.Current.FurnitureHackEnabled)
            {
                GCLatencyMode oldMode = GCSettings.LatencyMode;
                RuntimeHelpers.PrepareConstrainedRegions();
                try {
                    GCSettings.LatencyMode = GCLatencyMode.LowLatency;
                    jsonData = oSession.GetResponseBodyAsString();
                    var head = jsonData.Substring(0, jsonData.IndexOf("],\"api_mst_useitem"));
                    var tail = jsonData.Substring(jsonData.IndexOf("],\"api_mst_useitem"));
                    var api_mst_furnituregraph = jsonData.Substring(jsonData.IndexOf("api_mst_furnituregraph") + 24, jsonData.IndexOf("api_mst_useitem") - jsonData.IndexOf("api_mst_furnituregraph") - 26);
                    List<string> contain = new List<string>();
                    var furnituregraph_info = new Regex(@"{""api_id"":([0-9]+?).*?}").Matches(api_mst_furnituregraph);
                    foreach (Match result in furnituregraph_info)
                    {
                        contain.Add(result.Groups[1].Value);
                    }
                    var furniture_info = new Regex(@"({""api_id"":)([0-9]+?)(,""api_type"":[0-9]+?,""api_no"":)([0-9]+?),""api_title.*?}").Matches(jsonData);
                    foreach (Match result in furniture_info)
                    {
                        if (!contain.Contains(result.Groups[2].Value))
                        {
                            var fur_file = result.Groups[1].Value + result.Groups[2].Value + result.Groups[3].Value + result.Groups[4].Value + @",""api_filename"":""" + (int.Parse(result.Groups[4].Value) + 1).ToString("d3") + @""",""api_version"":""1""}";
                            head += "," + fur_file;
                        }
                    }
                    oSession.utilSetResponseBody(head + tail);
                }
                finally
                {
                    GCSettings.LatencyMode = oldMode;
                }
            }
        }
    }
}

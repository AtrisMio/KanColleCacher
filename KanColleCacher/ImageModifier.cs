using Codeplex.Data;
using d_f_32.KanColleCacher;
using d_f_32.KanColleCacher.Configuration;
using Fiddler;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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

            ModifyData.Items.Clear();
            jsonData = "";
        }
        /*
        private void setModifiedData(ModifyData data)
        {
            string graphStr = Regex.Match(jsonData, @"\{([^{]+?)" + data.FileName + @"([^}]+?)\}").Groups[0].Value;
            string sortNo = Regex.Match(graphStr, @"api_sortno"":(\d+)").Groups[1].Value;
            string infoStr = Regex.Match(jsonData, @"\{([^{]+?)api_sortno"":" + sortNo + @"([^}]+?)\}").Groups[0].Value;

            var graphReplaceStr = graphStr;
            var infoReplaceStr = infoStr;

            var temp = data.Data["ship_name"];
            if (temp != null && temp.Length > 0)
            {
                infoReplaceStr = Regex.Replace(infoReplaceStr, @"api_name"":""(.+?)""", @"api_name"":""" + temp + @"""");
            }

            var modList = new string[] { "boko_n", "boko_d",
                                         "kaisyu_n", "kaisyu_d",
                                         "kaizo_n", "kaizo_d",
                                         "map_n", "map_d",
                                         "ensyuf_n", "ensyuf_d",
                                         "ensyue_n",
                                         "battle_n", "battle_d",
                                         "weda", "wedb" };
            foreach (var mod in modList)
            {
                if(!data.Data.ContainsKey("boko_n_left")) break;

                temp = data.Data[mod + "_left"];
                if (temp != null && temp.Length > 0)
                {
                    graphReplaceStr = Regex.Replace(graphReplaceStr, mod + @""":\[([\d-]+),([\d-]+)\]", mod + @""":[" + temp + @",$2]");
                }

                temp = data.Data[mod + "_top"];
                if (temp != null && temp.Length > 0)
                {
                    graphReplaceStr = Regex.Replace(graphReplaceStr, mod + @""":\[([\d-]+),([\d-]+)\]", mod + @""":[$1," + temp + @"]");
                }
            }

            jsonData = jsonData.Replace(graphStr, graphReplaceStr);
            jsonData = jsonData.Replace(infoStr, infoReplaceStr);
        }
        */
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
            //if (!set.CacheEnabled) return;

            if (oSession.PathAndQuery.StartsWith("/kcsapi/api_start2") && Settings.Current.FurnitureHackEnabled)
            {
                jsonData = oSession.GetResponseBodyAsString();
                var head = jsonData.Substring(0, jsonData.IndexOf("\"api_mst_furnituregraph")+25);
                var tail = jsonData.Substring(jsonData.IndexOf(",\"api_mst_useitem"));
                var api_start2 = DynamicJson.Parse(jsonData.Substring(jsonData.IndexOf("api_mst_furniture") + 19, jsonData.IndexOf("api_mst_furnituregraph") - jsonData.IndexOf("api_mst_furniture") - 21));
                var api_start1 = DynamicJson.Parse(jsonData.Substring(7));
                ArrayList cotain = new ArrayList();
                ArrayList api_mst_furnituregraph = new ArrayList();
                foreach (var image in api_start1["api_data"]["api_mst_furnituregraph"])
                {
                    api_mst_furnituregraph.Add(new { api_id = image["api_id"], api_type = image["api_type"], api_no = image["api_no"],api_filename = ((int)(image["api_no"] + 1)).ToString("d3"), api_version = image["api_version"] });
                    cotain.Add(image["api_id"]);
                }
                
                foreach (var image in api_start2)
                {
                    if (!cotain.Contains(image["api_id"]))
                        api_mst_furnituregraph.Add(new { api_id = image["api_id"], api_type = image["api_type"], api_no = image["api_no"], api_filename = ((int)(image["api_no"] + 1)).ToString("d3"), api_version = "1" });
                    System.Console.WriteLine(image["api_title"]);
                }
                //api_start2["api_data"].api_mst_furniture = api_mst_furnituregraph.ToArray();
                var str = DynamicJson.Serialize(api_mst_furnituregraph.ToArray());
                oSession.utilSetResponseBody(head+str+tail);
                 //api_mst_furnituregraph

                    //ModifyData.Items.ForEach(x => setModifiedData(x));
                    //oSession.utilSetResponseBody(jsonData);
            }
        }
    }
    /*
    internal class ModifyData
    {
        public static List<ModifyData> Items = new List<ModifyData>();

        internal ModifyData(string path)
        {
            var st = path.LastIndexOf('\\') + 1;
            var ed = path.LastIndexOf(".config.ini");
            if (st > 0 && ed > st)
            {
                this.FileName = path.Substring(st, ed - st);
            }
            else
            {
                this.FileName = "Unknown";
            }

            this.Data = new Dictionary<string,string>();
            var parser = ConfigParser.ReadIniFile(path);
            if (parser["info"] != null)
            {
                this.Data.Add("ship_name", parser["info"]["ship_name"]);
            }
            else
            {
                this.Data.Add("ship_name", null);
            }
            if (parser["graph"] != null)
            {
                var modList = new string[] { "boko_n", "boko_d",
                                             "kaisyu_n", "kaisyu_d",
                                             "kaizo_n", "kaizo_d",
                                             "map_n", "map_d",
                                             "ensyuf_n", "ensyuf_d",
                                             "ensyue_n",
                                             "battle_n", "battle_d",
                                             "weda", "wedb" };
                foreach (var mod in modList)
                {
                    this.Data.Add(mod + "_left", parser["graph"][mod + "_left"]);
                    this.Data.Add(mod + "_top", parser["graph"][mod + "_top"]);
                }
            }
        }

        public string FileName { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
    */
}

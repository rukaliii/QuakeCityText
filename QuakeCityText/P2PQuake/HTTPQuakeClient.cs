using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QuakeCityText
{
    public class P2PQuakeClient
    {
        private static readonly HttpClient http = new();

        /// <summary>
        /// 履歴 API から最新1件の地震情報を取得
        /// </summary>
        public static async Task<JObject?> GetLatestEarthquakeAsync()
        {
            //string url = "https://api.p2pquake.net/v2/jma/quake?limit=1&offset=767";
            string url = "https://api.p2pquake.net/v2/history?codes=551&limit=1";
            try
            {
                string json = await http.GetStringAsync(url);

                // 配列で返るので JArray に変換
                var arr = JArray.Parse(json);
                if (arr.Count > 0)
                {
                    return (JObject)arr[0]; // 最初の要素を返す
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("取得失敗: " + ex.Message);
               System.Windows.Forms.MessageBox.Show("地震情報の取得に失敗しました。\n" + ex.Message, "エラー (P2PQuake JSON)", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            return null;
        }
    }
}

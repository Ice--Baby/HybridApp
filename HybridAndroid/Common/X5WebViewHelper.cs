using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Com.Tencent.Smtt.Export.External.Interfaces;
using HybridCommon.Agreement;
using HybridCommon.Context;
using HybridCommon.StaticResourceHelper;
using HybridCommon.Utils;
using Java.Interop;
using Newtonsoft.Json;
using Console = System.Console;
using CookieSyncManager = Com.Tencent.Smtt.Sdk.CookieSyncManager;
using IDownloadListener = Com.Tencent.Smtt.Sdk.IDownloadListener;
using IValueCallback = Com.Tencent.Smtt.Sdk.IValueCallback;
using IWebResourceRequest = Com.Tencent.Smtt.Export.External.Interfaces.IWebResourceRequest;
using Object = Java.Lang.Object;
using WebChromeClient = Com.Tencent.Smtt.Sdk.WebChromeClient;
using WebResourceError = Com.Tencent.Smtt.Export.External.Interfaces.WebResourceError;
using WebResourceResponse = Com.Tencent.Smtt.Export.External.Interfaces.WebResourceResponse;
using WebSettings = Com.Tencent.Smtt.Sdk.WebSettings;
using WebStorage = Com.Tencent.Smtt.Sdk.WebStorage;
using WebView = Com.Tencent.Smtt.Sdk.WebView;
using WebViewClient = Com.Tencent.Smtt.Sdk.WebViewClient;


namespace HybridAndroid.Common
{
    public class X5WebViewHelper : WebView
    {
        public X5WebViewHelper(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public X5WebViewHelper(Context p0, IAttributeSet p1, int p2, IDictionary<string, Object> p3, bool p4) : base(p0, p1, p2, p3, p4)
        {
        }

        public X5WebViewHelper(Context p0, IAttributeSet p1, int p2, bool p3) : base(p0, p1, p2, p3)
        {
        }

        public X5WebViewHelper(Context p0, IAttributeSet p1, int p2) : base(p0, p1, p2)
        {
        }

        public X5WebViewHelper(Context p0, IAttributeSet p1) : base(p0, p1)
        {
        }

        public X5WebViewHelper(Context p0) : base(p0)
        {
        }

        ///<summary>
        ///生成随机字符串 
        ///</summary>
        ///<param name="length">目标字符串的长度</param>
        ///<param name="useNum">是否包含数字，1=包含，默认为包含</param>
        ///<param name="useLow">是否包含小写字母，1=包含，默认为包含</param>
        ///<param name="useUpp">是否包含大写字母，1=包含，默认为包含</param>
        ///<param name="useSpe">是否包含特殊字符，1=包含，默认为不包含</param>
        ///<param name="custom">要包含的自定义字符，直接输入要包含的字符列表</param>
        ///<returns>指定长度的随机字符串</returns>
        public static string GetRandomString(int length = 20, bool useNum = true, bool useLow = true, bool useUpp = true, bool useSpe = false, string custom = "")
        {
            byte[] b = new byte[4];
            new System.Security.Cryptography.RNGCryptoServiceProvider().GetBytes(b);
            Random r = new Random(BitConverter.ToInt32(b, 0));
            string s = null, str = custom;
            if (useNum == true) { str += "0123456789"; }
            if (useLow == true) { str += "abcdefghijklmnopqrstuvwxyz"; }
            if (useUpp == true) { str += "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; }
            if (useSpe == true) { str += "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~"; }
            for (int i = 0; i < length; i++)
            {
                s += str.Substring(r.Next(0, str.Length - 1), 1);
            }
            return s;
        }

        /// <summary>
        /// WebViewKey,WebView
        /// </summary>
        public static Dictionary<string, X5WebViewHelper> WebViewAssets { get; set; }
        /// <summary>
        /// pageId,Activity
        /// </summary>
        public static Dictionary<string, Activity> PageActivity { get; set; }

        public static Dictionary<string,RelativeLayout> RelativeLayoutWebViewKey { get; set; }

        public static void AttachedWebView(string webViewKey, RelativeLayout rl)
        {
            if (WebViewAssets.ContainsKey(webViewKey))
            {
                if (RelativeLayoutWebViewKey == null)
                {
                    RelativeLayoutWebViewKey = new Dictionary<string, RelativeLayout>();
                }
                if (RelativeLayoutWebViewKey.ContainsKey(webViewKey))
                {
                    RelativeLayoutWebViewKey[webViewKey].RemoveView(WebViewAssets[webViewKey]);
                    WebViewAssets[webViewKey].Visibility = ViewStates.Invisible;
                    RelativeLayoutWebViewKey.Remove(webViewKey);
                }
                else
                {
                    rl.AddView(WebViewAssets[webViewKey]);
                    RelativeLayoutWebViewKey.Add(webViewKey,rl);

                    Task.Run(() =>
                    {
                        Thread.Sleep(600);
                        MainActivity.thisActivity.RunOnUiThread((() =>
                        {
                            X5WebViewHelper.WebViewAssets[webViewKey].Visibility = ViewStates.Visible;
                        }));
                    });

                }
            }
        }
        

        public static string WebViewInit(string pageId)
        {
            if (WebViewAssets == null)
            {
                WebViewAssets = new Dictionary<string, X5WebViewHelper>();
            }
            if (AnalyticAgreement.PageParam[pageId].IsRelative && WebViewAssets.ContainsKey(AnalyticAgreement.PageParam[pageId].ParentWebViewKey))
            {
                AnalyticAgreement.PageParam[pageId].WebViewKey = AnalyticAgreement.PageParam[pageId].ParentWebViewKey;
                AnalyticAgreement.PageParam[pageId].IsLoadFinished = AnalyticAgreement.PageParam[AnalyticAgreement.PageParam[pageId].ParentPageId].IsLoadFinished;
                return AnalyticAgreement.PageParam[pageId].WebViewKey;
            }

            if (string.IsNullOrWhiteSpace(AnalyticAgreement.PageParam[pageId].WebViewKey) || !WebViewAssets.ContainsKey(AnalyticAgreement.PageParam[pageId].WebViewKey))
            {
                var x5WebView = new X5WebViewHelper(MainActivity.thisActivity.Application.ApplicationContext);
                if (string.IsNullOrWhiteSpace(AnalyticAgreement.PageParam[pageId].WebViewKey))
                {
                    AnalyticAgreement.PageParam[pageId].WebViewKey = GetRandomString();
                }
                WebViewAssets.Add(AnalyticAgreement.PageParam[pageId].WebViewKey, x5WebView);
            }
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].ScrollBarStyle = ScrollbarStyles.InsideOverlay;
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].VerticalScrollBarEnabled = false;
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].HorizontalScrollBarEnabled = false;

            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.JavaScriptEnabled = true;
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.UseWideViewPort = true;
            //缓存模式
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.DomStorageEnabled = true;
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.SetAppCacheMaxSize(1024 * 1024 * 8);//设置缓冲大小，我设的是8M
            string appCacheDir = MainActivity.thisActivity.GetDir("appcache", FileCreationMode.Private).Path;
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.SetAppCachePath(appCacheDir);
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.SetAppCacheEnabled(true);
            string appDatabaseDir = MainActivity.thisActivity.GetDir("databases", FileCreationMode.Private).Path;
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.DatabasePath = appDatabaseDir;
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.DatabaseEnabled = true;
            string appGeolocationDir = MainActivity.thisActivity.GetDir("geolocation", FileCreationMode.Private).Path;
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.SetGeolocationDatabasePath(appGeolocationDir);
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.SetGeolocationEnabled(true);
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.AllowContentAccess = true;
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.AllowFileAccess = true;
            //网页自适应
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.SetLayoutAlgorithm(WebSettings.LayoutAlgorithm.SingleColumn);
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.LoadWithOverviewMode = true;
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.DefaultTextEncodingName = "utf-8";
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].SetWebViewClient(new ExtWebViewClient());
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].SetWebChromeClient(new ExtWebChromeClient());

            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.SetLayoutAlgorithm(WebSettings.LayoutAlgorithm.NarrowColumns);
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.SetSupportZoom(false);
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].Settings.SetSupportMultipleWindows(false);

            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].SetDownloadListener(new DownloadListener());
            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].SetOnLongClickListener(WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey]);

            var hostUrl = HybridCommon.Config.HtmlServerHost + "/index.html#/Empty/" + pageId;
            //var hostUrl = "file:///" + DeviceInfo.HtmlFolder + "/index.html#/Empty/" + pageId;

            WebViewAssets[AnalyticAgreement.PageParam[pageId].WebViewKey].LoadUrl(hostUrl);
            CookieSyncManager.CreateInstance(MainActivity.thisActivity.Application.ApplicationContext);
            CookieSyncManager.Instance.Sync();
            
            return AnalyticAgreement.PageParam[pageId].WebViewKey;
        }

        private static void X5WebViewHelper_LongClick(object sender, View.LongClickEventArgs e)
        {

        }

        private class ExtWebChromeClient : WebChromeClient
        {

            public override bool OnJsConfirm(WebView p0, string p1, string p2, IJsResult p3)
            {
                return base.OnJsConfirm(p0, p1, p2, p3);
            }

            View myVideoView;
            View myNormalView;
            IX5WebChromeClientCustomViewCallback callback;
            public override void OnShowCustomView(View view, int p1, IX5WebChromeClientCustomViewCallback customViewCallback)
            {
                base.OnShowCustomView(view, p1, customViewCallback);
            }

            public override void OnHideCustomView()
            {
                base.OnHideCustomView();
                if (callback != null)
                {
                    callback.OnCustomViewHidden();
                    callback = null;
                }
                if (myVideoView != null)
                {
                    ViewGroup viewGroup = (ViewGroup)myVideoView.Parent;
                    viewGroup.RemoveView(myVideoView);
                    viewGroup.AddView(myNormalView);
                }
            }

            public override bool OnShowFileChooser(WebView arg0, IValueCallback arg1, FileChooserParams arg2)
            {
                Console.WriteLine("onShowFileChooser");
                return base.OnShowFileChooser(arg0, arg1, arg2);
            }

            public override void OpenFileChooser(IValueCallback uploadFile, string acceptType, string captureType)
            {
                base.OpenFileChooser(uploadFile, acceptType, captureType);
            }

            public override bool OnJsAlert(WebView p0, string p1, string p2, IJsResult p3)
            {
                //这里写入你自定义的window alert
                return base.OnJsAlert(p0, p1, p2, p3);
            }

            //对应js 的通知弹框 ，可以用来实现js 和 android之间的通信
            public override void OnReceivedTitle(WebView p0, string p1)
            {
                base.OnReceivedTitle(p0, p1);
            }

            public override void OnReachedMaxAppCacheSize(long requiredStorage, long quota, WebStorage.IQuotaUpdater quotaUpdater)
            {
                quotaUpdater.UpdateQuota(requiredStorage * 2);
            }


        }

        private class ExtWebViewClient : WebViewClient
        {
            public override void OnPageFinished(WebView view, string url)
            {
            }
            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                return AnalyticAgreement.HandleAgreement(url);
            }

            public override WebResourceResponse ShouldInterceptRequest(WebView p0, IWebResourceRequest request)
            {
                return ShouldInterceptRequest(p0, request.Url.ToString());
            }

            public override WebResourceResponse ShouldInterceptRequest(WebView p0, string url)
            {
                if (url.Contains("/static/"))
                {
                    var filePath = url.Substring(url.LastIndexOf("/static/"), (url.Length - url.LastIndexOf("/static/")));
                    var extensionName = filePath.Substring(filePath.LastIndexOf(".") + 1,(filePath.Length - filePath.LastIndexOf(".") - 1));
                    if (!File.Exists(DeviceInfo.HtmlFolder + filePath))
                    {
                        Task.Run(() =>
                        {
                            new HtmlFileManage().DownloadFile(HybridCommon.Config.HtmlServerHost + filePath, DeviceInfo.HtmlFolder + filePath);
                        });
                    }
                    else
                    {
                        var fileStream = File.OpenRead(DeviceInfo.HtmlFolder + filePath);
                        var result = new WebResourceResponse();
                        result.Encoding = "UTF-8";
                        result.MimeType = MimeHelper.GetMime(extensionName);
                        result.Data = fileStream;
                        return result;
                    }
                }
                else if (url.Contains("/index.html#/Empty/"))
                {
                    var filePath = "/index.html";
                    if (!File.Exists(DeviceInfo.HtmlFolder + filePath))
                    {
                        Task.Run(() =>
                        {
                            new HtmlFileManage().DownloadFile(HybridCommon.Config.HtmlServerHost + filePath,
                                DeviceInfo.HtmlFolder + filePath);
                        });
                    }
                    else
                    {
                        var fileStream = File.OpenRead(DeviceInfo.HtmlFolder + filePath);
                        var result = new WebResourceResponse();
                        result.Encoding = "UTF-8";
                        result.MimeType = MimeHelper.GetMime("html");
                        result.Data = fileStream;
                        return result;
                    }
                }

                //if (url.Contains(DeviceInfo.HtmlFolder))
                //{

                //    url = url.Replace(DeviceInfo.HtmlFolder, "@");
                //    url = url.Substring(url.IndexOf("@")+1, url.Length - url.IndexOf("@")-1);
                //    url = HybridCommon.Config.HtmlServerHost + url;
                //}

                return base.ShouldInterceptRequest(p0, url);
            }

            public override void OnPageStarted(WebView p0, string p1, Bitmap p2)
            {
                base.OnPageStarted(p0, p1, p2);
            }

            public override void OnReceivedError(WebView p0, IWebResourceRequest p1, WebResourceError p2)
            {
                base.OnReceivedError(p0, p1, p2);
            }

            public override void OnReceivedError(WebView p0, int p1, string p2, string p3)
            {
                base.OnReceivedError(p0, p1, p2, p3);
            }

            public override void OnReceivedHttpError(WebView p0, IWebResourceRequest p1, WebResourceResponse p2)
            {
                base.OnReceivedHttpError(p0, p1, p2);
            }
        }

        public class DownloadListener : IDownloadListener
        {

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public IntPtr Handle { get; }
            public void OnDownloadStart(string p0, string p1, string p2, string p3, long p4)
            {
                throw new NotImplementedException();
            }


        }

        public override bool OnLongClick(View p0)
        {
            return false;
        }
        
    }
}
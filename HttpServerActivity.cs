// using System;
// using System.Linq;
// using System.Security.Cryptography.X509Certificates;
// using UMC.Data;
// using UMC.Data.Entities;
// using UMC.Net;
// using UMC.Security;
// using UMC.Web;
// using UMC.Web.UI;


// namespace UMC.Host
// {
//     [Mapping("Http", "Server", Auth = WebAuthType.Admin, Desc = "Http服务配置")]
//     class HttpServerActivity : WebActivity
//     {
//         string Expire(int now, int expireTime, string defaultStr)
//         {
//             var sExpireTime = defaultStr;// "未启用";
//             if (expireTime > 0)
//             {
//                 if (expireTime > now)
//                 {
//                     var t = new TimeSpan(0, 0, expireTime - now).TotalDays;
//                     if (t < 0)
//                     {
//                         sExpireTime = $"过期还剩{t:0.0}天";
//                     }
//                     else
//                     {
//                         sExpireTime = $"过期还剩{t:0}天";
//                     }
//                 }
//                 else
//                 {
//                     sExpireTime = "已过期";
//                 }
//             }
//             return sExpireTime;
//         }
//         public override void ProcessActivity(WebRequest request, WebResponse response)
//         {

//             var hosts = UMC.Data.Reflection.Configuration("host");
//             var model = this.AsyncDialog("Id", akey =>
//             {
//                 var form = request.SendValues ?? new WebMeta();
//                 if (form.ContainsKey("limit") == false)
//                 {
//                     this.Context.Send(new UISectionBuilder(request.Model, request.Command)
//                         .RefreshEvent($"{request.Model}.{request.Command}")
//                         .Builder(), true);

//                 }

//                 var ui = UISection.Create(new UITitle("服务配置"));

//                 var unix = hosts.Providers.Where(r =>
//                 {
//                     switch (r.Type)
//                     {
//                         case "unix":
//                             return true;
//                         default:
//                             return false;
//                     }
//                 });
//                 var provider = Data.WebResource.Instance().Provider;
//                 ui.AddCell("主协议", provider["scheme"] ?? "http", new UIClick("Domain").Send(request.Model, request.Command))
//                .AddCell("主域名", provider["domain"] ?? "未设置", new UIClick("Domain").Send(request.Model, request.Command)).AddCell("连接符", provider["union"] ?? ".", new UIClick("Domain").Send(request.Model, request.Command));

//                 ui.NewSection().AddCell("日志组件", new UIClick().Send("Proxy", "LogConf"));




//                 var http = hosts.Providers.Where(r =>
//                 {
//                     switch (r.Type)
//                     {
//                         case "unix":
//                         case "https":
//                             return false;
//                         default:
//                             return true;
//                     }
//                 });

//                 var httpUI = ui.NewSection();
//                 // httpUI.Header.Put("text", "Http服务");

//                 httpUI.AddCell("Http", new UIClick("Http").Send(request.Model, request.Command));
//                 if (http.Count() > 0)
//                 {
//                     foreach (var p in http)
//                     {
//                         var cell = UI.UI("端口", p.Attributes["port"] ?? "80");
//                         httpUI.Delete(cell, new UIEventText().Click(new UIClick(p.Name).Send(request.Model, request.Command)));
//                     }
//                 }
//                 else
//                 {
//                     UIDesc desc = new UIDesc("未配置Http服务");
//                     desc.Desc("{icon}\n{desc}").Put("icon", "\uf24a");
//                     desc.Style.Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60));
//                     httpUI.Add(desc);

//                 }

//                 var https = hosts.Providers.Where(r =>
//                 {
//                     switch (r.Type)
//                     {
//                         case "https":
//                             return true;
//                         default:
//                             return false;
//                     }
//                 });
//                 var httpsUI = ui.NewSection();
//                 httpsUI.AddCell("Https", new UIClick("Https").Send(request.Model, request.Command));
//                 if (https.Count() > 0)
//                 {
//                     foreach (var p in https)
//                     {
//                         var cell = UI.UI("端口", p.Attributes["port"] ?? "80");//, new UIClick(p.Name).Send(request.Model, request.Command));
//                         httpsUI.Delete(cell, new UIEventText().Click(new UIClick(akey, p.Name, "Type", "Del").Send(request.Model, request.Command)));
//                     }

//                 }
//                 else
//                 {
//                     UIDesc desc = new UIDesc("未配置Https服务");
//                     desc.Desc("{icon}\n{desc}").Put("icon", "\uf24a");
//                     desc.Style.Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60));
//                     httpsUI.Add(desc);
//                 }
//                 var sslUI = ui.NewSection();

//                 sslUI.AddCell("证书", new UIClick("Cert").Send(request.Model, request.Command));
//                 var now = UMC.Data.Utility.TimeSpan();
//                 var ls = Certificater.Certificates.Values.OrderBy(r =>
//                 {

//                     if (r.Certificate != null)
//                     {
//                         r.Time = Utility.TimeSpan(Convert.ToDateTime(r.Certificate.GetExpirationDateString()));

//                     }
//                     return r.Time;
//                 });
//                 foreach (var r in ls)
//                 {
//                     sslUI.AddCell(r.Name, Expire(now, r.Time, "正签发"));
//                 }
//                 if (ls.Count() == 0)
//                 {
//                     UIDesc desc = new UIDesc("未有SSL/TLS证书");
//                     desc.Desc("{icon}\n{desc}").Put("icon", "\uf24a");
//                     desc.Style.Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60));
//                     httpsUI.Add(desc);

//                 }


//                 ui.UIFootBar = new UIFootBar() { IsFixed = true };
//                 ui.UIFootBar.AddText(new UIEventText("申请证书").Click(new UIClick("ApplyCert").Send(request.Model, request.Command)),
//                     new UIEventText("重新加载").Click(new UIClick("Reload").Send(request.Model, request.Command)).Style(new UIStyle().BgColor()));

//                 // ui.UIFootBar = new UIFootBar() { IsFixed = true };
//                 // ui.UIFootBar.AddText(new UIEventText("新增服务").Click(new UIClick(new WebMeta(request.Arguments).Put(akey, "New")).Send(request.Model, request.Command)),
//                 //     new UIEventText("重新加载").Click(new UIClick(new WebMeta(request.Arguments).Put(akey, "Reload")).Send(request.Model, request.Command)).Style(new UIStyle().BgColor()));
//                 response.Redirect(ui);

//                 return this.DialogValue("none");
//             });
//             switch (model)
//             {
//                 case "Unix":
//                     hosts.Add(UMC.Data.Provider.Create("unix", "unix"));
//                     UMC.Data.Reflection.Configuration("host", hosts);
//                     this.Context.Send($"{request.Model}.{request.Command}", true);
//                     break;
//                 case "Domain":
//                     var provider = Data.WebResource.Instance().Provider;
//                     var Domains = this.AsyncDialog("Domain", r =>
//                     {
//                         var fm = new UIFormDialog() { Title = "本地站点" };

//                         fm.AddText("主域名", "domain", provider["domain"]);
//                         var union = provider["union"] ?? ".";
//                         var scheme = provider["scheme"] ?? "http";
//                         fm.AddRadio("主协议", "scheme").Put("http", "http", scheme == "http").Put("https", "https", scheme == "https");
//                         fm.AddRadio("连接符", "union").Put("-", "-", union == "-").Put(".", ".", union == ".");
//                         fm.Submit("确认", $"{request.Model}.{request.Command}");
//                         return fm;
//                     });
//                     provider.Attributes["union"] = Domains["union"];
//                     provider.Attributes["scheme"] = Domains["scheme"];
//                     provider.Attributes["domain"] = Domains["domain"];

//                     var pc = UMC.Data.Reflection.Configuration("assembly") ?? new ProviderConfiguration();

//                     pc.Add(provider);
//                     UMC.Data.Reflection.Configuration("assembly", pc);
//                     this.Context.Send($"{request.Model}.{request.Command}", true);
//                     break;
//                 case "Site":
//                     var Site = this.AsyncDialog("Site", r =>
//                     {
//                         return new UITextDialog(hosts.ProviderType ?? HttpMimeServier.Server) { Title = "服务站点" };
//                     });
//                     hosts.ProviderType = Site;
//                     UMC.Data.Reflection.Configuration("host", hosts);
//                     this.Context.Send($"{request.Model}.{request.Command}", true);
//                     break;
//                 case "Reload":
//                     var msg = HttpMimeServier.Load(hosts);
//                     if (msg.Length > 0)
//                     {
//                         this.Prompt("提示", msg);
//                     }
//                     else
//                     {
//                         this.Prompt("已经成功加载");
//                     }
//                     break;
//                 case "Cert":
//                     {

//                         var httpPorts2 = this.AsyncDialog("Cert", r =>
//                         {
//                             var fm = new UIFormDialog() { Title = "证书" };
//                             fm.AddText("域名", "Domain", String.Empty);
//                             fm.AddTextarea("公钥", "publicKey", String.Empty).Put("Rows", 10).PlaceHolder("以-----BEGIN CERTIFICATE-----开始的证书").Put("tip", "公钥证书");
//                             fm.AddTextarea("私钥", "privateKey", String.Empty).Put("Rows", 10).PlaceHolder("以-----BEGIN RSA PRIVATE KEY-----开始的证书").Put("tip", "私钥证书");
//                             fm.Submit("确认添加", $"{request.Model}.{request.Command}");
//                             return fm;
//                         });

//                         var certs = UMC.Data.Reflection.Configuration("certs");
//                         try
//                         {
//                             var x509 = X509Certificate2.CreateFromPem(httpPorts2["publicKey"], httpPorts2["privateKey"]);
//                             if (Utility.Parse(x509.GetExpirationDateString(), DateTime.MinValue) < DateTime.Now)
//                             {
//                                 x509.Dispose();
//                                 this.Prompt("此证书已过期");
//                             }
//                             var p = UMC.Data.Provider.Create(httpPorts2["Domain"], "Cert");
//                             p.Attributes["publicKey"] = httpPorts2["publicKey"];
//                             p.Attributes["privateKey"] = httpPorts2["privateKey"];
//                             certs.Add(p);
//                             UMC.Net.Certificater.Certificates[p.Name] = new Certificater { Name = p.Name, Status = 1, Certificate = x509 };
//                             UMC.Data.Reflection.Configuration("certs", certs);
//                             this.Context.Send($"{request.Model}.{request.Command}.Cert", true);
//                         }
//                         catch
//                         {
//                             this.Prompt("证书不正确");
//                         }
//                         break;
//                     }
//                 case "ApplyCert":
//                     {
//                         var host = UIDialog.AsyncDialog(this.Context, "Domain", g =>
//                         {
//                             var fm = new UIFormDialog() { Title = "申请证书" };
//                             fm.AddText("域名", "Domain", String.Empty);
//                             fm.Submit("确认申请", $"{request.Model}.{request.Command}.Cert");
//                             return fm;
//                         });

//                         if (System.Text.RegularExpressions.Regex.IsMatch(host, @"^([a-z0-9]([a-z0-9\-]{0,61}[a-z0-9])?\.)+[a-z0-9]{1,6}$") == false)
//                         {
//                             this.Prompt("域名格式不正确");
//                         }

//                         var secret = WebResource.Instance().Provider["appSecret"];
//                         if (String.IsNullOrEmpty(secret))
//                         {
//                             this.Prompt("当前版本未登记注册", false);
//                             response.Redirect("System", "License");

//                         }
//                         var webr2 = new Uri(APIProxy.Uri, "Certificater").WebRequest();
//                         UMC.Proxy.Utility.Sign(webr2, new System.Collections.Specialized.NameValueCollection(), secret);

//                         var webr = webr2.Post(new WebMeta().Put("type", "apply", "domain", host));

//                         var str = webr.ReadAsString();
//                         if (webr.StatusCode == System.Net.HttpStatusCode.OK)
//                         {
//                             var hs = JSON.Deserialize<WebMeta>(str);
//                             this.Context.Send($"{request.Model}.{request.Command}.Cert", false);
//                             this.Prompt("提示", hs["msg"] ?? "正在签发证书");

//                         }
//                         else
//                         {
//                             this.Prompt("错误", $"请确保域名“{host}”解释到服务器，并开放80端口");
//                         }
//                     }
//                     break;
//                 case "Http":
//                     var httpPort = UIDialog.AsyncDialog(this.Context, "Port", g =>
//                        {
//                            var fm = new UIFormDialog() { Title = "新增Http" };
//                            fm.AddNumber("端口", "Port", String.Empty);
//                            fm.Submit("确认", $"{request.Model}.{request.Command}");
//                            return fm;
//                        });
//                     if (Utility.IntParse(httpPort, 0) > 0)
//                     {
//                         var p = UMC.Data.Provider.Create(httpPort, "http");
//                         p.Attributes["port"] = httpPort;
//                         hosts.Add(p);
//                         UMC.Data.Reflection.Configuration("host", hosts);
//                         this.Context.Send($"{request.Model}.{request.Command}", true);
//                     }
//                     else
//                     {
//                         this.Prompt("请输入正确的端口号");
//                     }
//                     break;
//                 case "Https":

//                     var httpsPort = UIDialog.AsyncDialog(this.Context, "Port", g =>
//                        {
//                            var fm = new UIFormDialog() { Title = "新增Https" };
//                            fm.AddNumber("端口", "Port", String.Empty);
//                            fm.Submit("确认", $"{request.Model}.{request.Command}");
//                            return fm;
//                        });
//                     if (Utility.IntParse(httpsPort, 0) > 0)
//                     {
//                         var p = UMC.Data.Provider.Create(httpsPort, "https");
//                         p.Attributes["port"] = httpsPort;

//                         hosts.Add(p);
//                         UMC.Data.Reflection.Configuration("host", hosts);
//                         this.Context.Send($"{request.Model}.{request.Command}", true);
//                     }
//                     else
//                     {
//                         this.Prompt("请输入正确的端口号");
//                     }
//                     break;
//                 default:

//                     var pr = hosts[model];
//                     if (pr != null)
//                     {
//                         hosts.Remove(model);
//                         UMC.Data.Reflection.Configuration("host", hosts);
//                         this.Context.Send($"{request.Model}.{request.Command}", true);
//                     }
//                     break;
//             }

//         }
//     }
// }
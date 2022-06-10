using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PasswordGenerator;
using System.IO;
using Captcha.Models;
using System.Text.Json;
using Utils;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Captcha.Controllers
{
    [ApiController]
    [Route("captcha")]
    public class CaptchaController : ControllerBase
    {
        private static readonly string CryptoKey = "TEST";

        private readonly ILogger<CaptchaController> _logger;

        public CaptchaController(ILogger<CaptchaController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("token")]
        public String getToken()
        {
            // 產生一個 5 個字元的亂碼字串
            var pwd = new Password(5).IncludeNumeric();
            var Captcha = pwd.Next();

            // 封裝字符和過期時間
            CaptchaModel captchaModel = new CaptchaModel();
            captchaModel.Code = Captcha;

            //設定過期時間為3分鐘
            captchaModel.Exp = DateTime.Now.AddMinutes(3);

            //轉換json字串
            string jsonString = JsonSerializer.Serialize(captchaModel);
            System.Console.WriteLine(jsonString);

            //AES加密
            string token = StringEncrypt.aesEncryptBase64(jsonString, CryptoKey);

            //取代特殊符號回傳         

            return base64url_encode(token);
        }

        [HttpGet]
        [Route("img")]
        public IActionResult Get(String token)
        {
            var captchaModel = vaildate(token);
            if (captchaModel == null)
            {
                return Ok("token is expired");
            }



            var byteImage = GenerateCaptcha(captchaModel.Code);
            return File(byteImage, "image/jpeg");



        }



        [HttpGet]
        [Route("audio")]
        public IActionResult AudioGet(String token)
        {
            var captchaModel = vaildate(token);
            if (captchaModel == null)
            {
                return Ok("token is expired");
            }

            var code = captchaModel.Code;
            var compose = new byte[]{}; 
            foreach (var item in code)
            {
                byte[] bytes = System.IO.File.ReadAllBytes($"Audio/{item}.mp3");
                compose = Combine(compose,bytes);
            }
            


            return File(compose, "audio/mpeg");



        }

        public static byte[] Combine(byte[] first, byte[] second)
        {
            return first.Concat(second).ToArray();
        }

        [HttpPut]
        [Route("vaild")]
        /// <summary>
        /// 驗證輸入數字是否正確
        /// </summary>
        /// <param name="token">token</param>
        /// <param name="code">數字</param>
        /// <returns>回傳Model</returns>
        public String vaild([FromHeader] String token, [FromHeader] String code)
        {
            //判斷是否輸入
            if (String.IsNullOrEmpty(token))
            {
                return null;
            }

            //還原
            token = base64url_decode(token);

            //AES解密
            var jsonString = StringEncrypt.aesDecryptBase64(token, CryptoKey);

            //轉換物件
            var captchaModel = JsonSerializer.Deserialize<CaptchaModel>(jsonString);


            //取得資料
            var Captcha = captchaModel.Code;
            var Exp = captchaModel.Exp;


            //判斷時間是否過期
            if (DateTime.Now > Exp)
            {
                return null;
            }
            if (captchaModel.Code != code)
            {
                return null;
            }

            return "OK";
        }

        [HttpPut]
        [Route("vaildate")]
        /// <summary>
        /// 驗證token是否正確
        /// </summary>
        /// <param name="token"></param>
        /// <returns>回傳Model</returns>
        public CaptchaModel vaildate([FromHeader] String token)
        {
            //判斷是否輸入
            if (String.IsNullOrEmpty(token))
            {
                return null;
            }

            //還原
            token = base64url_decode(token);

            //AES解密
            var jsonString = StringEncrypt.aesDecryptBase64(token, CryptoKey);

            //轉換物件
            var captchaModel = JsonSerializer.Deserialize<CaptchaModel>(jsonString);


            //取得資料
            var Captcha = captchaModel.Code;
            var Exp = captchaModel.Exp;


            //判斷時間是否過期
            if (DateTime.Now > Exp)
            {
                return null;
            }

            return captchaModel;
        }

        /// <summary>
        /// 將base64編碼取代會造成問題的部份
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private String base64url_encode(String s)
        {
            s = s.Replace('+', '-');
            s = s.Replace('/', '_');
            return s;
        }

        /// <summary>
        /// 將base64編碼還原
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private String base64url_decode(String s)
        {
            s = s.Replace('-', '+');
            s = s.Replace('_', '/');
            return s;
        }



        /// <summary>
        /// 輸入code 產生Captcha
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private byte[] GenerateCaptcha(String code)
        {
            var ww = 60;
            var hh = 20;
            // 設定要出現在圖片上的文字字型、大小與樣式
            var font = SystemFonts.CreateFont("Courier New", 16, FontStyle.Bold);


            using (Image<Rgba32> image = new Image<Rgba32>(ww, hh))   //畫布大小
            {
                image.Mutate(x =>
                {
                    //設定圖片背景
                    var imgProc = x.BackgroundColor(Color.Cornsilk);

                    //逐個畫字
                    x.DrawText(code, font, Color.Black, new PointF(6, 3));

                    
                    Random rdn = new Random();

                    // 圖片干擾
                    for (int i = 0; i < 10; i++)
                    {
                        var pen = new Pen(Color.Black, 1);
                        var p1 = new PointF(rdn.Next(ww), rdn.Next(hh));
                        var p2 = new PointF(rdn.Next(ww), rdn.Next(hh));

                        x.DrawLines(pen, p1, p2);
                    }

                    // 產生雜點
                    for (int i = 0; i < 80; i++)
                    {
                        var pen = new Pen(Color.Brown, 1);
                        var p1 = new PointF(rdn.Next(ww), rdn.Next(hh));
                        var p2 = new PointF(p1.X + 1f, p1.Y + 1f);

                        x.DrawLines(pen, p1, p2);
                    }

                    
                });

                byte[] byteImage = ImageToBytes(image);

                return byteImage;
                
            }
         


            


 
            // //產生擾亂弧線
            // int x2 = 0;
            // int y2 = 0;
            // int x3 = 0;
            // int y3 = 0;
            // for (int i = 0; i < 15; i++)
            // {
            //     x1 = rdn.Next(_bmp.Width - intNoiseWidth);
            //     y1 = rdn.Next(_bmp.Height - intNoiseHeight);
            //     x2 = rdn.Next(1, intNoiseWidth);
            //     y2 = rdn.Next(1, intNoiseHeight);
            //     x3 = rdn.Next(0, 45);
            //     y3 = rdn.Next(-270, 270);
            //     _graphics.DrawArc(new Pen(Brushes.Gray), x1, y1, x2, y2, x3, y3);
            // }


            // 輸出之前 Captcha 圖示
            // Response.ContentType = "image/gif";
        }

        private byte[] ImageToBytes(Image<Rgba32> imageIn)
        {
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream();
                imageIn.Save(ms, new JpegEncoder { Quality = 85 });
                return  ms.ToArray();
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            finally
            {
                ms.Close();
            }
        }
    }
}

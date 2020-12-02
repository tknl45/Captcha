using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Drawing;
using PasswordGenerator;
using System.Drawing.Imaging;
using System.IO;
using Captcha.Models;
using System.Text.Json;
using Utils;

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
        public String getToken(){
            // 產生一個 5 個字元的亂碼字串
            var pwd = new Password(5).IncludeNumeric();
            var Captcha = pwd.Next();

            // 封裝字符和過期時間
            CaptchaModel captchaModel = new CaptchaModel();
            captchaModel.Code = Captcha;

            //設定過期時間為一分鐘
            captchaModel.Exp = DateTime.Now.AddMinutes(1);

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
            if( captchaModel == null){
                 return Ok("token is expired");
            }
            

            
            var byteImage = GenerateCaptcha(captchaModel.Code);
            return File(byteImage, "image/jpeg");

            
         
        }

        [HttpPut]
        [Route("vaildate")]
        /// <summary>
        /// 驗證token是否正確
        /// </summary>
        /// <param name="token"></param>
        /// <returns>回傳Model</returns>
        public CaptchaModel vaildate([FromHeader]String token){
            //判斷是否輸入
            if(String.IsNullOrEmpty(token)){
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
            if(DateTime.Now > Exp){
                return null;
            }

            return captchaModel;
        }

        /// <summary>
        /// 將base64編碼取代會造成問題的部份
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private String base64url_encode(String s) {
            s = s.Replace('+', '-');
            s = s.Replace('/', '_');
            return s;
        }

        /// <summary>
        /// 將base64編碼還原
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private String base64url_decode(String s) {
            s = s.Replace('-', '+');
            s = s.Replace('_', '/');
            return s;
        }

        

        /// <summary>
        /// 輸入code 產生Captcha
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private byte[] GenerateCaptcha(String code){
            // (封裝 GDI+ 點陣圖) 新增一個 Bitmap 物件，並指定寬、高
            Bitmap _bmp = new Bitmap(60, 20);
            
            // (封裝 GDI+ 繪圖介面) 所有繪圖作業都需透過 Graphics 物件進行操作
            Graphics _graphics = Graphics.FromImage(_bmp);

            // 設定圖片背景
            _graphics.Clear(Color.Cornsilk);

            // 如果想啟用「反鋸齒」功能，可以將以下這行取消註解
            //_graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

            // 設定要出現在圖片上的文字字型、大小與樣式
            Font _font = new Font("Courier New", 12, FontStyle.Bold);
            
            //產生雜點
            Random rdn = new Random();
            int intNoiseWidth = 25;
            int intNoiseHeight = 15;
            int x1 = 0;
            int y1 = 0;

            for (int i = 0; i < 80; i++)
            {
                x1 = rdn.Next(0, _bmp.Width);
                y1 = rdn.Next(0, _bmp.Height);
                _bmp.SetPixel(x1, y1, Color.Brown);
            }

            //產生擾亂弧線
            int x2 = 0;
            int y2 = 0;
            int x3 = 0;
            int y3 = 0;
            for (int i = 0; i < 15; i++)
            {
                x1 = rdn.Next(_bmp.Width - intNoiseWidth);
                y1 = rdn.Next(_bmp.Height - intNoiseHeight);
                x2 = rdn.Next(1, intNoiseWidth);
                y2 = rdn.Next(1, intNoiseHeight);
                x3 = rdn.Next(0, 45);
                y3 = rdn.Next(-270, 270);
                _graphics.DrawArc(new Pen(Brushes.Gray), x1, y1, x2, y2, x3, y3);
            }


            

            // 將亂碼字串「繪製」到之前產生的 Graphics 「繪圖板」上
            _graphics.DrawString(Convert.ToString(code), 
                _font, Brushes.Black, 3, 3);

            // 輸出之前 Captcha 圖示
            Response.ContentType = "image/gif";
            
            byte[] byteImage = BitmapToBytes(_bmp);
            
            return byteImage;
        }

        private byte[] BitmapToBytes(Bitmap Bitmap) 
        { 
            MemoryStream ms = null; 
            try 
            { 
                ms = new MemoryStream(); 
                Bitmap.Save(ms, ImageFormat.Gif); 
                byte[] byteImage = new Byte[ms.Length]; 
                byteImage = ms.ToArray(); 
                return byteImage; 
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

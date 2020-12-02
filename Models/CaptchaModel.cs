using System;

namespace Captcha.Models
{
    public class CaptchaModel
    {
        /// <summary>
        /// 認證碼
        /// </summary>
        /// <value></value>
        public string Code{get;set;}

        /// <summary>
        /// 過期時間
        /// </summary>
        /// <value></value>
        public DateTime Exp{get;set;}
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Facebook;
using System.Net;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace Parallelity.Facebook
{
    public class FacebookManager
    {
        private const string AppId = "434529523339914";
        private const string ExtendedPermissions = "publish_stream,photo_upload";
        private static string _accessToken;

        public static bool Login()
        {
            _accessToken = null;

            FacebookLoginDialog dialog = new FacebookLoginDialog(AppId, ExtendedPermissions);
            dialog.ShowDialog();

            if (dialog.FacebookOAuthResult != null)
            {
                if (dialog.FacebookOAuthResult.IsSuccess)
                    _accessToken = dialog.FacebookOAuthResult.AccessToken;
                else
                    MessageBox.Show(dialog.FacebookOAuthResult.ErrorDescription);
            }

            return dialog.FacebookOAuthResult != null && dialog.FacebookOAuthResult.IsSuccess;
        }

        public static bool IsLoggedIn()
        {
            return _accessToken != null;
        }

        public static void Logout()
        {
            FacebookClient fb = new FacebookClient();
            Uri logoutUrl = fb.GetLogoutUrl(new
            {
                access_token = _accessToken,
                next = "https://www.facebook.com/connect/login_success.html"
            });

            WebClient client = new WebClient();
            client.DownloadString(logoutUrl);

            _accessToken = null;
        }

        public static bool PublishPhoto(Image photo, String message)
        {
            if (!IsLoggedIn() && !Login())
                return false;

            MemoryStream ms = new MemoryStream();
            photo.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            byte[] imageBytes = ms.ToArray();

            FacebookClient fb = new FacebookClient(_accessToken);
            dynamic result = fb.Post("me/photos",
                new
                {
                    message = message,
                    source = new FacebookMediaObject
                    {
                        ContentType = "image/png",
                        FileName = "image.png"
                    }.SetValue(imageBytes)
                });

            return result != null;
        }
    }
}

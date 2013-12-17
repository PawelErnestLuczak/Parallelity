using System;
using System.Dynamic;
using System.Windows.Forms;
using Facebook;

namespace Parallelity.Facebook
{
    public partial class FacebookLoginDialog : Form
    {
        private readonly Uri _loginUrl;
        protected readonly FacebookClient _fb;
        public FacebookOAuthResult FacebookOAuthResult { get; private set; }

        public FacebookLoginDialog(string appId, string extendedPermissions)
            : this(new FacebookClient(), appId, extendedPermissions)
        {
        }

        public FacebookLoginDialog(FacebookClient fb, string appId, string extendedPermissions)
        {
            if (fb == null)
                throw new ArgumentNullException("fb");

            if (string.IsNullOrWhiteSpace(appId))
                throw new ArgumentNullException("appId");

            _fb = fb;
            _loginUrl = GenerateLoginUrl(appId, extendedPermissions);

            InitializeComponent();
        }

        private Uri GenerateLoginUrl(string appId, string extendedPermissions)
        {
            dynamic parameters = new ExpandoObject();
            parameters.client_id = appId;
            parameters.redirect_uri = "https://www.facebook.com/connect/login_success.html";

            parameters.response_type = "token";
            parameters.display = "popup";

            if (!string.IsNullOrWhiteSpace(extendedPermissions))
                parameters.scope = extendedPermissions;

            return _fb.GetLoginUrl(parameters);
        }

        private void FacebookLoginDialog_Load(object sender, EventArgs e)
        {
            webBrowser1.Navigate(_loginUrl.AbsoluteUri);
        }

        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            FacebookOAuthResult oauthResult;

            if (_fb.TryParseOAuthCallbackUrl(e.Url, out oauthResult))
            {
                FacebookOAuthResult = oauthResult;
                DialogResult = FacebookOAuthResult.IsSuccess ? DialogResult.OK : DialogResult.No;
            }
            else
            {
                FacebookOAuthResult = null;
            }
        }
    }
}

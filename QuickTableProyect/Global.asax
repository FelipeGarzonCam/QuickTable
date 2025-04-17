using System;
using System.IO;
using System.Web;

public class Global : HttpApplication
{
    protected void Application_Start(object sender, EventArgs e)
    {
        AppDomain.CurrentDomain.SetData("DataDirectory", 
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data"));
    }
}

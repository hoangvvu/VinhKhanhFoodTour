using Android.App;
using Android.Runtime;

namespace VKFoodTour.Mobile
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
            // Catch all unhandled managed exceptions before they cross the JNI boundary
            // and become an opaque JavaProxyThrowable with no stack trace.
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                Android.Util.Log.Error("VKFoodTour", $"UNHANDLED EXCEPTION: {ex}");
            };

            // Catch fire-and-forget Task exceptions that were never observed.
            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                Android.Util.Log.Error("VKFoodTour", $"UNOBSERVED TASK EXCEPTION: {args.Exception}");
                args.SetObserved(); // prevent process termination
            };
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}

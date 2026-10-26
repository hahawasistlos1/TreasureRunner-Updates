package com.treasurerunner.updates;
import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.provider.Settings;
import java.io.File;
public final class UpdateBridge {
    public static void shareInvite(Activity activity,String text) {
        activity.runOnUiThread(() -> {
            Intent intent=new Intent(Intent.ACTION_SEND);
            intent.setType("text/plain");intent.putExtra(Intent.EXTRA_TEXT,text);
            activity.startActivity(Intent.createChooser(intent,"TreasureRunner-Freund einladen"));
        });
    }
    public static void shareLevel(Activity activity,String text) {
        Intent intent=new Intent(Intent.ACTION_SEND);
        intent.setType("text/plain");intent.putExtra(Intent.EXTRA_TEXT,text);
        activity.startActivity(Intent.createChooser(intent,"TreasureRunner-Level teilen"));
    }
    public static String directory(Context context) { File dir=new File(context.getCacheDir(),"treasurerunner-updates");dir.mkdirs();return dir.getAbsolutePath(); }
    public static boolean install(Activity activity) {
        if(Build.VERSION.SDK_INT>=26 && !activity.getPackageManager().canRequestPackageInstalls()) {
            activity.startActivity(new Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES,Uri.parse("package:"+activity.getPackageName())));return false;
        }
        File apk=new File(directory(activity),"treasurerunner-update.apk");
        if(!apk.isFile())return false;
        Intent intent=new Intent(Intent.ACTION_VIEW);
        intent.setDataAndType(Uri.parse("content://"+activity.getPackageName()+".updates/treasurerunner-update.apk"),"application/vnd.android.package-archive");
        intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        activity.startActivity(intent);return true;
    }
}

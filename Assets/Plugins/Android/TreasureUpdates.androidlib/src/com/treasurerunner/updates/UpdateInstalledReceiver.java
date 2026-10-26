package com.treasurerunner.updates;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import java.io.File;
public final class UpdateInstalledReceiver extends BroadcastReceiver {
    @Override public void onReceive(Context context,Intent intent){
        if(!Intent.ACTION_MY_PACKAGE_REPLACED.equals(intent.getAction()))return;
        File dir=new File(UpdateBridge.directory(context));
        for(String name:new String[]{"treasurerunner-update.apk","treasurerunner-update.apk.partial","pending.json"})new File(dir,name).delete();
        android.util.Log.i("TreasureRunnerUpdate","Installed update APK cache removed");
        UpdateNotifications.ensureScheduled(context);
        UpdateNotifications.scheduleFirst(context);
    }
}

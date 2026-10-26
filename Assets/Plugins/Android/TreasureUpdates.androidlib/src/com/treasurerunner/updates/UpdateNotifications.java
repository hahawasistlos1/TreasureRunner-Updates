package com.treasurerunner.updates;

import android.app.Activity;
import android.app.job.JobScheduler;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.job.JobInfo;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Build;
import android.provider.Settings;

/** Native API only: can run without starting Unity or loading game assets. */
public final class UpdateNotifications {
    public static final int PERIODIC_JOB=0x545255, FIRST_JOB=0x545256;
    private static final int NOTICE=0x545257;
    private static final String CHANNEL="treasurerunner_updates_v1", PREFS="treasurerunner_update_notices";
    private static final Object LOCK=new Object();
    private static volatile boolean visible;
    private UpdateNotifications() {}
    static SharedPreferences prefs(Context c) { return c.getSharedPreferences(PREFS,Context.MODE_PRIVATE); }
    static boolean optedIn(Context c) { return prefs(c).getBoolean("enabled",false) && prefs(c).getBoolean("channel_enabled",false); }
    static boolean gameVisible() { return visible; }
    public static void setVisible(boolean value) { visible=value; }
    static NotificationManager manager(Context c) { return c.getSystemService(NotificationManager.class); }
    static void channel(Context c) {
        NotificationManager manager=manager(c);
        if(manager==null) return;
        NotificationChannel channel=new NotificationChannel(CHANNEL,"Spielupdates",NotificationManager.IMPORTANCE_DEFAULT);
        channel.setDescription("Neue TreasureRunner-Versionen mit Änderungshinweisen");
        manager.createNotificationChannel(channel);
    }
    public static boolean permitted(Context c) {
        if(Build.VERSION.SDK_INT>=33 && c.checkSelfPermission("android.permission.POST_NOTIFICATIONS")!=PackageManager.PERMISSION_GRANTED) return false;
        NotificationManager manager=manager(c);
        if(manager==null || !manager.areNotificationsEnabled()) return false;
        NotificationChannel channel=manager.getNotificationChannel(CHANNEL);
        return channel==null || channel.getImportance()!=NotificationManager.IMPORTANCE_NONE;
    }
    public static boolean enabled(Context c) { return prefs(c).getBoolean("enabled",false); }
    public static void configure(Context c,boolean allowedChannel) {
        synchronized(LOCK) {
            prefs(c).edit().putBoolean("channel_enabled",allowedChannel).apply();
            ensureScheduled(c);
        }
    }
    public static void setEnabled(Context c,boolean value) {
        synchronized(LOCK) {
            boolean was=enabled(c);
            prefs(c).edit().putBoolean("enabled",value).apply();
            ensureScheduled(c);
            if(value && !was) scheduleFirst(c);
        }
    }
    public static void ensureScheduled(Context c) {
        JobScheduler scheduler=c.getSystemService(JobScheduler.class);
        if(scheduler==null) return;
        if(!optedIn(c) || !permitted(c)) {
            scheduler.cancel(PERIODIC_JOB);scheduler.cancel(FIRST_JOB);
            NotificationManager manager=manager(c);if(manager!=null) manager.cancel(NOTICE);
            return;
        }
        channel(c);
        String installed=installed(c),posted=prefs(c).getString("posted","");
        if(!posted.isEmpty() && !UpdateNoticePolicy.newer(posted,installed)) manager(c).cancel(NOTICE);
        JobInfo old=scheduler.getPendingJob(PERIODIC_JOB);
        if(old!=null && old.isPeriodic() && old.getIntervalMillis()==UpdateNoticePolicy.PERIOD_MS) return;
        try {
            int result=scheduler.schedule(job(c,PERIODIC_JOB).setPeriodic(UpdateNoticePolicy.PERIOD_MS,UpdateNoticePolicy.FLEX_MS).build());
            prefs(c).edit().putBoolean("schedule_failed",result!=JobScheduler.RESULT_SUCCESS).apply();
        } catch(RuntimeException denied) { prefs(c).edit().putBoolean("schedule_failed",true).apply(); }
    }
    private static JobInfo.Builder job(Context c,int id) {
        return new JobInfo.Builder(id,new ComponentName(c,UpdateNotificationJob.class))
            .setPersisted(true).setRequiredNetworkType(JobInfo.NETWORK_TYPE_ANY).setRequiresBatteryNotLow(true)
            .setBackoffCriteria(60L*60*1000,JobInfo.BACKOFF_POLICY_EXPONENTIAL);
    }
    public static void scheduleFirst(Context c) {
        if(!optedIn(c) || !permitted(c)) return;
        JobScheduler scheduler=c.getSystemService(JobScheduler.class);
        if(scheduler==null || scheduler.getPendingJob(FIRST_JOB)!=null) return;
        try { scheduler.schedule(job(c,FIRST_JOB).setMinimumLatency(30000).build()); }
        catch(RuntimeException denied) { android.util.Log.w("TreasureRunnerUpdate","Initial background check could not be scheduled"); }
    }
    static String installed(Context c) {
        try { return c.getPackageManager().getPackageInfo(c.getPackageName(),0).versionName; }
        catch(PackageManager.NameNotFoundException absent) { return ""; }
    }
    public static void acknowledge(Context c,String version) {
        if(UpdateNoticePolicy.version(version)==null) return;
        synchronized(LOCK) {
            String seen=prefs(c).getString("seen","");
            if(UpdateNoticePolicy.version(seen)==null || UpdateNoticePolicy.newer(version,seen)) prefs(c).edit().putString("seen",version).apply();
            String posted=prefs(c).getString("posted","");
            if(!posted.isEmpty() && !UpdateNoticePolicy.newer(posted,version)) { NotificationManager manager=manager(c);if(manager!=null) manager.cancel(NOTICE); }
        }
    }
    static boolean post(Context c,String version,String body) {
        synchronized(LOCK) {
            if(!optedIn(c) || !permitted(c) || visible || !UpdateNoticePolicy.shouldNotify(version,installed(c),prefs(c).getString("seen",""),false,false,true)) return false;
            Intent launch=c.getPackageManager().getLaunchIntentForPackage(c.getPackageName());
            if(launch==null) return false;
            launch.setAction(Intent.ACTION_VIEW).setData(Uri.parse(UpdateNoticePolicy.OPEN_UPDATE))
                .addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP|Intent.FLAG_ACTIVITY_SINGLE_TOP);
            PendingIntent tap=PendingIntent.getActivity(c,NOTICE,launch,PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);
            channel(c);
            int icon=c.getResources().getIdentifier("tr_update_notification","drawable",c.getPackageName());
            if(icon==0) icon=android.R.drawable.stat_sys_download_done;
            String title="TreasureRunner "+version+" verfügbar",summary=UpdateNoticePolicy.summary(body);
            Notification notification=new Notification.Builder(c,CHANNEL).setSmallIcon(icon).setContentTitle(title)
                .setContentText("Neue Spielversion · Tippen für Änderungen und Download")
                .setStyle(new Notification.BigTextStyle().bigText(summary)).setContentIntent(tap)
                .setAutoCancel(true).setOnlyAlertOnce(true).setCategory(Notification.CATEGORY_STATUS).build();
            try { manager(c).notify(NOTICE,notification); }
            catch(SecurityException denied) { return false; }
            prefs(c).edit().putString("seen",version).putString("posted",version).apply();
            return true;
        }
    }
    public static String status(Context c) {
        if(!enabled(c)) return "Ausgeschaltet · nur im geöffneten Spiel prüfen.";
        if(!prefs(c).getBoolean("channel_enabled",false)) return "Update-Kanal ist nicht aktiviert.";
        if(!permitted(c)) return "Android-Benachrichtigungen sind noch nicht erlaubt oder blockiert.";
        if(prefs(c).getBoolean("schedule_failed",false)) return "Android konnte die Hintergrundprüfung nicht einplanen. Erneut aktivieren.";
        return "Aktiv · ungefähr alle 6 Stunden, sobald Android es erlaubt.\n"+prefs(c).getString("last_result","Erste Hintergrundprüfung steht noch aus.");
    }
    public static void openSettings(Activity activity) {
        activity.runOnUiThread(()->activity.startActivity(new Intent(Settings.ACTION_APP_NOTIFICATION_SETTINGS).putExtra(Settings.EXTRA_APP_PACKAGE,activity.getPackageName())));
    }
}

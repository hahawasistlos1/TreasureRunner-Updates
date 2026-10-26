package com.treasurerunner.updates;

import android.app.job.JobParameters;
import android.app.job.JobService;
import android.os.Handler;
import android.os.Looper;
import org.json.JSONArray;
import org.json.JSONObject;
import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.atomic.AtomicBoolean;

/** Bounded metadata request on a worker thread. Never downloads an APK or starts Unity. */
public final class UpdateNotificationJob extends JobService {
    private static final class Run {
        final AtomicBoolean cancelled=new AtomicBoolean();
        volatile HttpURLConnection connection;
        Thread thread;
        void cancel() { cancelled.set(true);if(connection!=null) connection.disconnect();if(thread!=null) thread.interrupt(); }
    }
    private final Map<Integer,Run> runs=new HashMap<>();
    private final Handler main=new Handler(Looper.getMainLooper());
    @Override public boolean onStartJob(JobParameters parameters) {
        if(!UpdateNotifications.optedIn(this) || !UpdateNotifications.permitted(this) || UpdateNotifications.gameVisible()) return false;
        Run run=new Run();
        if(!runs.isEmpty()) return false;
        runs.put(parameters.getJobId(),run);
        run.thread=new Thread(()->check(parameters,run),"TreasureRunner-update-metadata");
        run.thread.start();return true;
    }
    private void check(JobParameters parameters,Run run) {
        boolean retry=false;
        String result="Hintergrundprüfung: keine neue vollständige Version.";
        try {
            URL url=new URL(UpdateNoticePolicy.API);
            android.net.Network network=android.os.Build.VERSION.SDK_INT>=28 ? parameters.getNetwork() : null;
            HttpURLConnection connection=(HttpURLConnection)(network!=null ? network.openConnection(url) : url.openConnection());run.connection=connection;
            if(run.cancelled.get()) return;
            connection.setConnectTimeout(10000);connection.setReadTimeout(10000);connection.setInstanceFollowRedirects(false);
            connection.setRequestProperty("User-Agent","TreasureRunner-Android-UpdateNotice");
            connection.setRequestProperty("Accept","application/vnd.github+json");
            int response=connection.getResponseCode();
            if(response!=200) {
                retry=response==429 || response>=500;
                result=response==404 ? "Noch keine Spielversion veröffentlicht." : "Update-Server nicht erreichbar · später erneut prüfen.";
                return;
            }
            long length=connection.getContentLengthLong();
            if(length>UpdateNoticePolicy.MAX_RESPONSE_BYTES) { result="Update-Antwort zu groß · Benachrichtigung verworfen.";return; }
            ByteArrayOutputStream bytes=new ByteArrayOutputStream();
            long deadline=android.os.SystemClock.elapsedRealtime()+20000;
            try(InputStream input=connection.getInputStream()) {
                byte[] buffer=new byte[8192];int count;
                while((count=input.read(buffer))!=-1) {
                    if(run.cancelled.get() || Thread.currentThread().isInterrupted()) return;
                    if(bytes.size()+count>UpdateNoticePolicy.MAX_RESPONSE_BYTES || android.os.SystemClock.elapsedRealtime()>deadline) {
                        result="Update-Antwort verworfen · Begrenzung überschritten.";return;
                    }
                    bytes.write(buffer,0,count);
                }
            }
            if(run.cancelled.get()) return;
            JSONObject release=new JSONObject(new String(bytes.toByteArray(),StandardCharsets.UTF_8));
            // Missing status flags are not accepted as a public stable release.
            boolean draft=release.getBoolean("draft"),prerelease=release.getBoolean("prerelease");
            String version=release.optString("tag_name","");boolean assetValid=false;
            JSONArray assets=release.optJSONArray("assets");
            if(assets!=null && assets.length()<=1000) for(int i=0;i<assets.length();i++) {
                JSONObject asset=assets.optJSONObject(i);
                if(asset!=null && UpdateNoticePolicy.allowedAsset(asset.optString("name"),asset.optString("browser_download_url"),asset.optLong("size",0),asset.optString("digest"))) { assetValid=true;break; }
            }
            if(UpdateNoticePolicy.shouldNotify(version,UpdateNotifications.installed(this),UpdateNotifications.prefs(this).getString("seen",""),draft,prerelease,assetValid)
                && !run.cancelled.get() && UpdateNotifications.post(this,version,release.optString("body",""))) result="Neue Version "+version+" gemeldet.";
        } catch(Exception failed) {
            if(!run.cancelled.get()) { retry=failed instanceof java.io.IOException;result="Offline oder ungültige Antwort · später erneut prüfen."; }
        } finally {
            if(run.connection!=null) run.connection.disconnect();
            if(!run.cancelled.get()) UpdateNotifications.prefs(this).edit().putString("last_result",result).putLong("last_check",System.currentTimeMillis()).apply();
            final boolean reschedule=retry;
            main.post(()->{
                if(runs.get(parameters.getJobId())==run) {
                    runs.remove(parameters.getJobId());
                    if(!run.cancelled.get()) jobFinished(parameters,reschedule);
                }
            });
        }
    }
    @Override public boolean onStopJob(JobParameters parameters) {
        Run run=runs.remove(parameters.getJobId());if(run!=null) run.cancel();
        return UpdateNotifications.optedIn(this) && UpdateNotifications.permitted(this);
    }
    @Override public void onDestroy() {
        for(Run run:runs.values()) run.cancel();runs.clear();super.onDestroy();
    }
}

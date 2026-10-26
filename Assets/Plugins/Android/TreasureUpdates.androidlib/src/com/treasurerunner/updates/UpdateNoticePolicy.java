package com.treasurerunner.updates;

import java.net.URI;

/** Android-independent policy, shared by the background check and local JVM tests. */
public final class UpdateNoticePolicy {
    public static final String OWNER = "hahawasistlos1";
    public static final String REPOSITORY = "TreasureRunner-Updates";
    public static final String API = "https://api.github.com/repos/"+OWNER+"/"+REPOSITORY+"/releases/latest";
    public static final String OPEN_UPDATE = "treasurerunner://updates";
    public static final int MAX_RESPONSE_BYTES = 524288;
    public static final long PERIOD_MS = 6L*60*60*1000;
    public static final long FLEX_MS = 60L*60*1000;
    private UpdateNoticePolicy() {}

    static int[] version(String text) {
        if(text==null || text.length()>48) return null;
        int offset=0;
        while(offset<text.length() && (text.charAt(offset)=='v' || text.charAt(offset)=='V')) offset++;
        String[] parts=text.substring(offset).split("\\.",-1);
        if(parts.length<2 || parts.length>4) return null;
        int[] result={-1,-1,-1,-1};
        try {
            for(int i=0;i<parts.length;i++) {
                if(!parts[i].matches("[0-9]{1,10}")) return null;
                result[i]=Integer.parseInt(parts[i]);
            }
        } catch(NumberFormatException invalid) { return null; }
        return result;
    }
    public static boolean newer(String candidate,String installed) {
        int[] a=version(candidate),b=version(installed);
        if(a==null || b==null) return false;
        for(int i=0;i<4;i++) if(a[i]!=b[i]) return a[i]>b[i];
        return false;
    }
    public static boolean allowedAsset(String name,String url,long bytes,String digest) {
        if(!"TreasureRunner.apk".equals(name) || bytes<=0 || bytes>268435456L || digest==null || !digest.matches("sha256:[0-9a-fA-F]{64}")) return false;
        try {
            URI uri=new URI(url);
            String path=uri.getRawPath();
            return "https".equals(uri.getScheme()) && "github.com".equals(uri.getHost()) && (uri.getPort()==-1 || uri.getPort()==443)
                && uri.getUserInfo()==null && uri.getRawQuery()==null && uri.getRawFragment()==null
                && path!=null && path.startsWith("/"+OWNER+"/"+REPOSITORY+"/releases/download/") && path.endsWith(".apk")
                && !uri.getPath().contains("..") && !path.toLowerCase(java.util.Locale.ROOT).contains("%2f") && !path.toLowerCase(java.util.Locale.ROOT).contains("%5c");
        } catch(Exception invalid) { return false; }
    }
    public static boolean shouldNotify(String candidate,String installed,String seen,boolean draft,boolean prerelease,boolean assetValid) {
        return !draft && !prerelease && assetValid && newer(candidate,installed) && (version(seen)==null || newer(candidate,seen));
    }
    public static String summary(String body) {
        if(body==null || body.trim().isEmpty()) return "Verbesserungen und Fehlerkorrekturen. Tippen für Details und Download.";
        String plain=body.replaceAll("[\\p{Cc}&&[^\\n\\t]]","").replaceAll("[#*`]","").trim();
        return plain.length()>1200 ? plain.substring(0,1200)+" …" : plain;
    }
}

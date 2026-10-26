package com.treasurerunner.identity;

import android.app.Activity;
import android.os.CancellationSignal;
import android.util.Base64;
import androidx.core.content.ContextCompat;
import androidx.credentials.ClearCredentialStateRequest;
import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.ClearCredentialException;
import androidx.credentials.exceptions.GetCredentialCancellationException;
import androidx.credentials.exceptions.GetCredentialException;
import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;
import com.google.firebase.FirebaseApp;
import com.google.firebase.FirebaseNetworkException;
import com.google.firebase.FirebaseOptions;
import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.auth.FirebaseUser;
import com.google.firebase.auth.GoogleAuthProvider;
import com.google.firebase.firestore.FirebaseFirestore;
import com.google.firebase.firestore.FirebaseFirestoreSettings;
import com.google.firebase.firestore.Source;
import java.nio.charset.StandardCharsets;
import java.security.SecureRandom;
import java.util.HashMap;
import java.util.Map;
import org.json.JSONObject;

/** Only uid and an explicitly chosen nickname cross into Unity. No provider profile or raw errors. */
public final class IdentityBridge {
    private static FirebaseAuth auth;
    private static FirebaseFirestore profiles;
    private static CredentialManager manager;
    private static CancellationSignal pending;
    private static int sequence;
    private static boolean exchanging;
    private static String webClientId;
    private static boolean validNickname(String value) {
        return value!=null && value.equals(value.trim()) && value.length()>=3 && value.length()<=20
            && value.matches("[A-Za-z0-9ÄÖÜäöüß_. -]+") && value.matches(".*[A-Za-z0-9ÄÖÜäöüß].*");
    }
    private static boolean current(int request,String uid) {
        FirebaseUser user=auth==null ? null : auth.getCurrentUser();
        return request==sequence && (uid==null || (user!=null && uid.equals(user.getUid())));
    }
    private static void send(String receiver,int request,String state,String uid,String nickname,String error) {
        if(request!=sequence)return;
        try {
            JSONObject result=new JSONObject();result.put("request",request);result.put("state",state);
            if(uid!=null)result.put("uid",uid);
            if(nickname!=null)result.put("nickname",nickname);
            if(error!=null)result.put("error",error);
            Class.forName("com.unity3d.player.UnityPlayer").getMethod("UnitySendMessage",String.class,String.class,String.class)
                .invoke(null,receiver,"OnIdentityResult",result.toString());
        } catch(Exception ignored) { /* Do not log credentials, account data or provider exceptions. */ }
    }
    private static void error(String receiver,int request,Exception exception,String fallback) {
        send(receiver,request,"error",null,null,exception instanceof FirebaseNetworkException ? "network" : fallback);
    }
    public static void initialize(Activity activity,String receiver,int request,String config) {
        activity.runOnUiThread(()->{
            sequence=request;
            try {
                JSONObject options=new JSONObject(config);if(!options.optBoolean("enabled"))throw new IllegalStateException();
                webClientId=options.getString("googleWebClientId");
                FirebaseApp app=null;for(FirebaseApp existing:FirebaseApp.getApps(activity))if(existing.getName().equals("TreasureRunnerIdentity"))app=existing;
                if(app==null)app=FirebaseApp.initializeApp(activity,new FirebaseOptions.Builder()
                    .setApiKey(options.getString("firebaseApiKey")).setApplicationId(options.getString("firebaseAppId"))
                    .setProjectId(options.getString("firebaseProjectId")).build(),"TreasureRunnerIdentity");
                auth=FirebaseAuth.getInstance(app);profiles=FirebaseFirestore.getInstance(app);
                profiles.setFirestoreSettings(new FirebaseFirestoreSettings.Builder().setPersistenceEnabled(false).build());
                manager=CredentialManager.create(activity);
                FirebaseUser user=auth.getCurrentUser();
                if(user==null)send(receiver,request,"guest",null,null,null);else readProfile(receiver,request,user.getUid());
            } catch(Exception exception) { error(receiver,request,exception,"configuration"); }
        });
    }
    private static void readProfile(String receiver,int request,String uid) {
        profiles.collection("players").document(uid).get(Source.SERVER).addOnSuccessListener(document->{
            if(!current(request,uid))return;
            String nickname=document.exists() ? document.getString("nickname") : null;
            if(document.exists() && !validNickname(nickname)){send(receiver,request,"error",null,null,"profile");return;}
            send(receiver,request,document.exists() ? "ready" : "nickname",uid,nickname,null);
        }).addOnFailureListener(exception->error(receiver,request,exception,"profile"));
    }
    public static void signIn(Activity activity,String receiver,int request,String unused) {
        activity.runOnUiThread(()->{
            sequence=request;
            if(auth==null || manager==null){send(receiver,request,"error",null,null,"configuration");return;}
            if(exchanging){send(receiver,request,"error",null,null,"signin");return;}
            if(pending!=null)pending.cancel();pending=new CancellationSignal();
            byte[] random=new byte[32];new SecureRandom().nextBytes(random);String nonce=Base64.encodeToString(random,Base64.URL_SAFE|Base64.NO_WRAP|Base64.NO_PADDING);
            GetSignInWithGoogleOption google=new GetSignInWithGoogleOption.Builder(webClientId).setNonce(nonce).build();
            GetCredentialRequest query=new GetCredentialRequest.Builder().addCredentialOption(google).build();
            manager.getCredentialAsync(activity,query,pending,ContextCompat.getMainExecutor(activity),new CredentialManagerCallback<GetCredentialResponse,GetCredentialException>() {
                @Override public void onResult(GetCredentialResponse result) {
                    if(!current(request,null))return;
                    try {
                        Credential credential=result.getCredential();
                        if(!(credential instanceof CustomCredential) || !GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL.equals(credential.getType()))throw new IllegalStateException();
                        String token=GoogleIdTokenCredential.createFrom(((CustomCredential)credential).getData()).getIdToken();
                        String[] pieces=token.split("\\.");if(pieces.length!=3)throw new IllegalStateException();
                        JSONObject claims=new JSONObject(new String(Base64.decode(pieces[1],Base64.URL_SAFE|Base64.NO_WRAP|Base64.NO_PADDING),StandardCharsets.UTF_8));
                        if(!nonce.equals(claims.optString("nonce")))throw new IllegalStateException();
                        // Firebase verifies the signed Google credential server-side before a UID is trusted.
                        exchanging=true;
                        auth.signInWithCredential(GoogleAuthProvider.getCredential(token,null)).addOnSuccessListener(value->{
                            exchanging=false;if(!current(request,null)){auth.signOut();return;}
                            if(value.getUser()!=null)readProfile(receiver,request,value.getUser().getUid());
                        }).addOnFailureListener(exception->{exchanging=false;error(receiver,request,exception,"signin");});
                    } catch(Exception exception) { error(receiver,request,exception,"signin"); }
                }
                @Override public void onError(GetCredentialException exception) {
                    send(receiver,request,"error",null,null,exception instanceof GetCredentialCancellationException ? "cancelled" : "signin");
                }
            });
        });
    }
    public static void saveNickname(Activity activity,String receiver,int request,String nickname) {
        activity.runOnUiThread(()->{
            sequence=request;FirebaseUser user=auth==null ? null : auth.getCurrentUser();
            if(user==null || profiles==null || !validNickname(nickname)){send(receiver,request,"error",null,null,"profile");return;}
            String uid=user.getUid();Map<String,Object> fields=new HashMap<>();fields.put("schema",1);fields.put("nickname",nickname);
            profiles.collection("players").document(uid).set(fields).addOnSuccessListener(unused->{
                if(current(request,uid))send(receiver,request,"ready",uid,nickname,null);
            }).addOnFailureListener(exception->error(receiver,request,exception,"profile"));
        });
    }
    public static void signOut(Activity activity,String receiver,int request,String unused) {
        activity.runOnUiThread(()->{
            sequence=request;if(pending!=null){pending.cancel();pending=null;}if(auth!=null)auth.signOut();
            if(manager!=null)manager.clearCredentialStateAsync(new ClearCredentialStateRequest(),null,ContextCompat.getMainExecutor(activity),new CredentialManagerCallback<Void,ClearCredentialException>() {
                @Override public void onResult(Void result) { }
                @Override public void onError(ClearCredentialException exception) { }
            });
            send(receiver,request,"guest",null,null,null);
        });
    }
    private IdentityBridge() { }
}

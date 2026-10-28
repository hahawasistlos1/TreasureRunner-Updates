package com.treasurerunner.identity;

import android.app.Activity;
import com.google.firebase.auth.FirebaseAuth;
import com.google.firebase.auth.FirebaseUser;
import com.google.firebase.firestore.FirebaseFirestore;
import com.google.firebase.firestore.FieldValue;
import com.google.firebase.firestore.Source;
import com.google.firebase.firestore.WriteBatch;
import java.util.HashMap;
import java.util.Map;
import org.json.JSONObject;

/** Uses the existing named Firebase app. Credentials and provider profiles never cross to Unity. */
public final class CommunityReviewBridge {
    private static void send(String receiver,int request,boolean ok) {
        try {
            JSONObject result=new JSONObject();result.put("request",request);result.put("state",ok ? "ok" : "error");
            Class.forName("com.unity3d.player.UnityPlayer").getMethod("UnitySendMessage",String.class,String.class,String.class)
                .invoke(null,receiver,"OnReviewResult",result.toString());
        } catch(Exception ignored) { }
    }
    public static void saveReview(Activity activity,String receiver,int request,String payload) {
        activity.runOnUiThread(()->{
            try {
                FirebaseFirestore db=IdentityBridge.reviewDatabase();FirebaseAuth auth=IdentityBridge.reviewAuth();
                FirebaseUser user=auth==null ? null : auth.getCurrentUser();
                JSONObject input=new JSONObject(payload);String key=input.getString("key"),comment=input.getString("comment");int stars=input.getInt("stars"),revision=input.getInt("revision");
                if(db==null || user==null || !key.matches("[a-f0-9]{64}") || stars<1 || stars>5 || revision<1 || revision>10000 || comment.length()>400)throw new IllegalArgumentException();
                String uid=user.getUid();
                db.collection("players").document(uid).get(Source.SERVER).addOnSuccessListener(profile->{
                    if(auth.getCurrentUser()==null || !uid.equals(auth.getCurrentUser().getUid()) || !profile.exists()){send(receiver,request,false);return;}
                    Map<String,Object> fields=new HashMap<>();fields.put("nickname",profile.getString("nickname"));fields.put("stars",stars);fields.put("comment",comment);fields.put("revision",revision);fields.put("updatedAt",FieldValue.serverTimestamp());
                    Map<String,Object> clock=new HashMap<>();clock.put("at",FieldValue.serverTimestamp());clock.put("level",key);
                    WriteBatch batch=db.batch();batch.set(db.collection("reviewWriters").document(uid),clock);
                    batch.set(db.collection("levelReviews").document(key).collection("entries").document(uid),fields);
                    batch.commit().addOnSuccessListener(unused->send(receiver,request,true)).addOnFailureListener(error->send(receiver,request,false));
                }).addOnFailureListener(error->send(receiver,request,false));
            } catch(Exception ignored){send(receiver,request,false);}
        });
    }
    public static void deleteReview(Activity activity,String receiver,int request,String key) {
        activity.runOnUiThread(()->{
            try {
                FirebaseFirestore db=IdentityBridge.reviewDatabase();FirebaseAuth auth=IdentityBridge.reviewAuth();FirebaseUser user=auth==null ? null : auth.getCurrentUser();
                if(db==null || user==null || !key.matches("[a-f0-9]{64}"))throw new IllegalArgumentException();
                db.collection("levelReviews").document(key).collection("entries").document(user.getUid()).delete()
                    .addOnSuccessListener(unused->send(receiver,request,true)).addOnFailureListener(error->send(receiver,request,false));
            } catch(Exception ignored){send(receiver,request,false);}
        });
    }
    private CommunityReviewBridge() { }
}

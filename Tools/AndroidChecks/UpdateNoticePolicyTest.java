package com.treasurerunner.updates;

public final class UpdateNoticePolicyTest {
    private static int checks;
    private static final String URL="https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.9.0/TreasureRunner.apk";
    private static final String SHA="sha256:"+new String(new char[64]).replace('\0','a');
    private static void check(boolean value,String label) { if(!value) throw new AssertionError(label);checks++;System.out.println("PASS: "+label); }
    public static void main(String[] args) {
        check(UpdateNoticePolicy.newer("v0.9.0","0.8.0"),"newer stable release");
        check(UpdateNoticePolicy.newer("V0.10.0","0.9.9"),"numeric version comparison rather than string ordering");
        check(!UpdateNoticePolicy.newer("v0.8.0","0.8.0"),"installed release never produces notice");
        check(!UpdateNoticePolicy.newer("v0.7.0","0.8.0"),"older release rejected");
        check(UpdateNoticePolicy.newer("0.8.0.1","0.8.0"),"four component version");
        for(String invalid:new String[]{null,"","0","v","0.9.0-beta","0.9.0+2","0.9.0.1.2","0.-9.0","0.9.","0..9","0.2147483648.0","0.99999999999999999999.0","0.9.0\n"})
            check(!UpdateNoticePolicy.newer(invalid,"0.8.0"),"invalid tag rejected: "+invalid);
        check(!UpdateNoticePolicy.newer("0.9.0","invalid"),"unknown installed version fails closed");
        check(UpdateNoticePolicy.allowedAsset("TreasureRunner.apk",URL,51255902,SHA),"official complete APK metadata");
        for(String invalid:new String[]{URL.replace("https:","http:"),URL.replace("github.com","github.com.evil.test"),URL.replace("github.com","user@github.com"),URL.replace("hahawasistlos1","someone"),URL.replace("TreasureRunner-Updates","Other"),URL+"?redirect=evil",URL+"#fragment",URL.replace("github.com","github.com:8443"),URL.replace("/v0.9.0/","/%2e%2e/"),URL.replace("/v0.9.0/","/v0.9.0%2f/"),URL.replace("/v0.9.0/","/v0.9.0%5C/"),"not a url"})
            check(!UpdateNoticePolicy.allowedAsset("TreasureRunner.apk",invalid,51255902,SHA),"foreign or ambiguous download URL rejected");
        check(!UpdateNoticePolicy.allowedAsset("Source.zip",URL,3,SHA),"source archive cannot trigger APK notice");
        check(!UpdateNoticePolicy.allowedAsset("TreasureRunner.apk",URL,0,SHA),"zero-byte APK rejected");
        check(!UpdateNoticePolicy.allowedAsset("TreasureRunner.apk",URL,268435457L,SHA),"oversized APK rejected");
        check(!UpdateNoticePolicy.allowedAsset("TreasureRunner.apk",URL,3,null),"missing digest rejected");
        check(!UpdateNoticePolicy.allowedAsset("TreasureRunner.apk",URL,3,SHA.replace('a','z')),"nonhex digest rejected");
        check(UpdateNoticePolicy.shouldNotify("v0.9.0","0.8.0","",false,false,true),"first new version notified");
        check(!UpdateNoticePolicy.shouldNotify("v0.9.0","0.8.0","0.9.0",false,false,true),"same release not repeated after restart");
        check(!UpdateNoticePolicy.shouldNotify("v0.9.0","0.8.0","1.0.0",false,false,true),"older than previously seen not repeated");
        check(UpdateNoticePolicy.shouldNotify("v0.10.0","0.8.0","0.9.0",false,false,true),"following release notified");
        check(!UpdateNoticePolicy.shouldNotify("v0.9.0","0.8.0","",true,false,true),"draft rejected");
        check(!UpdateNoticePolicy.shouldNotify("v0.9.0","0.8.0","",false,true,true),"prerelease rejected");
        check(!UpdateNoticePolicy.shouldNotify("v0.9.0","0.8.0","",false,false,false),"incomplete release rejected");
        check(UpdateNoticePolicy.summary(null).contains("Details"),"fallback text for absent changelog");
        check(UpdateNoticePolicy.summary("# Neu\n*Schwert*\u0000").equals("Neu\nSchwert"),"notification gets plain bounded text");
        check(UpdateNoticePolicy.summary(new String(new char[2000]).replace('\0','x')).length()<=1202,"long changelog bounded");
        System.out.println("ALL "+checks+" BACKGROUND UPDATE POLICY CHECKS PASSED");
    }
}

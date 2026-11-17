#if UNITY_ANDROID && !UNITY_EDITOR // stop auto formatter removing unused using.
using UnityEngine;
#endif

namespace Script
{
    public class AndroidTV
    {
        
        public static bool? isTv = null;

        public static bool IsAndroidOrFireTv()
        {
            if (!isTv.HasValue)
            {
#if UNITY_EDITOR
                return false;
#elif UNITY_ANDROID
                try
                {
                    using (AndroidJavaObject bridge = new AndroidJavaObject("AndroidBridge"))
                    {
                        isTv = bridge.Call<bool>("isTV");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Couldn't Retrieve IsAndroidOrFireTv" + e);
                    isTv = false;
                }
#endif
            }

            return (bool)isTv;
        }
    }
}
import android.app.Activity;
import android.app.UiModeManager;
import android.content.Context;
import android.content.res.Configuration;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

public class AndroidBridge {
public boolean isTV() {
Log.d("Java", "isTV");

Activity activity = UnityPlayer.currentActivity;
UiModeManager uiModeManager = (UiModeManager) activity.getSystemService(Context.UI_MODE_SERVICE);
if (uiModeManager.getCurrentModeType() == Configuration.UI_MODE_TYPE_TELEVISION) {
return true;
} else {
return false;
}
}
}
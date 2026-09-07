using UnityEngine;
using UnityEngine.UI;

namespace MapleClient.GameView.UI
{
    /// <summary>A portrait uses the same animation with a fixed origin, independent of world facing.</summary>
    [DefaultExecutionOrder(1200)]
    public sealed class ClassicNpcPortrait : MonoBehaviour
    {
        private ClassicNpcAnimator actor;private Image image;private float center,scale,bottom;
        public void Bind(ClassicNpcAnimator value,float x,float baseline=155,float width=100,float height=111)
        {
            actor=value;center=x;bottom=baseline;image=GetComponent<Image>();scale=1;
            if(actor?.Frame!=null)
                scale=Mathf.Min(1,width/actor.PortraitBounds.width,height/actor.PortraitBounds.height);
            Present();
        }
        private void LateUpdate()=>Present();
        public void Present()
        {
            if(actor==null || actor.Frame==null || image==null)return;
            var frame=actor.Frame;image.sprite=frame.Sprite;image.enabled=true;
            ClassicUI.Place(image.rectTransform,center-(actor.PortraitBounds.center.x+frame.Origin.x)*scale,bottom-(actor.PortraitBounds.yMax+frame.Origin.y)*scale,
                frame.Sprite.rect.width*scale,frame.Sprite.rect.height*scale);
        }
    }
}

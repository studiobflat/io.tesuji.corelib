using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GOVisualize : MonoBehaviour
{
   #if UNITY_EDITOR
   [Serializable] public class VElement
   {
      public bool enable = true;
      public virtual void Draw(Transform t, float duration)
      {
         
      }
   }

   [Serializable] public class VAxis : VElement
   {
      [Range(0.1f, 10f)] public float scale = 1f;

      public bool x = true;
      public bool y = true;
      public bool z = true;
      
      public override void Draw(Transform t, float duration)
      {
         if (!enable) return;
         var p = t.position;
         if (x) Debug.DrawLine(p, p + t.right * scale, Color.red, duration);
         if (y) Debug.DrawLine(p, p + t.up * scale, Color.green, duration);
         if (z) Debug.DrawLine(p, p + t.forward * scale, Color.blue, duration);
      }
   }

   [Serializable] public class VPosition : VElement
   {
      private Vector3 lastPos;
      public Color color = Color.cyan;
      
      public override void Draw(Transform t, float duration)
      {
         if (!enable) return;

         var p = t.position;
         Debug.DrawLine(lastPos, p, color, duration);
         lastPos = p;
      }
   }

   [Serializable] public class VRigidBody : VElement
   {
      public Color color = Color.magenta;
      [Range(0.1f, 10f)] public float scale = 1f;
      
      public override void Draw(Transform t, float duration)
      {
         if (!enable) return;

         var r = t.GetComponent<Rigidbody>();
         if (!r) return;

         var p = t.position;
         Debug.DrawLine(p, p + r.velocity * scale, color, duration);
      }
   }
   
   [Serializable] public class VVelocityRight : VElement
   {
      public Transform customRight;
      public Color color = Color.magenta;
      [Range(0.1f, 10f)] public float scale = 1f;
      
      public override void Draw(Transform t, float duration)
      {
         if (!enable) return;

         var r = t.GetComponent<Rigidbody>();
         if (!r) return;

         var p = t.position;
         var cr = customRight != null ? customRight : t;
         Debug.DrawLine(p, p + cr.right * r.velocity.magnitude * scale, color, duration);
      }
   }


   [Range(1f, 10f)] public float duration = 1f;
   public VPosition posDrawer;
   public VAxis axisDrawer;
   public VRigidBody rigidBodyDrawer;
   public VVelocityRight velocityRightDrawer;
   
   private void OnDrawGizmos()
   {
      posDrawer.Draw(transform, duration);
      axisDrawer.Draw(transform, duration);
      rigidBodyDrawer.Draw(transform, duration);
      velocityRightDrawer.Draw(transform, duration);
   }
   
   #endif
}

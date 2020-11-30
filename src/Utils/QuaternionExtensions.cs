using UnityEngine;

public static class QuaternionExtensions
{
    // Source: https://gist.github.com/maxattack/4c7b4de00f5c1b95a33b
    public static Quaternion SmoothDamp(this Quaternion current, Quaternion target, ref Quaternion currentVelocity, float smoothTime)
    {
        // account for double-cover
        var dot = Quaternion.Dot(current, target);
        var multi = dot > 0f ? 1f : -1f;
        target.x *= multi;
        target.y *= multi;
        target.z *= multi;
        target.w *= multi;
        // smooth damp (nlerp approx)
        var result = new Vector4(
            Mathf.SmoothDamp(current.x, target.x, ref currentVelocity.x, smoothTime),
            Mathf.SmoothDamp(current.y, target.y, ref currentVelocity.y, smoothTime),
            Mathf.SmoothDamp(current.z, target.z, ref currentVelocity.z, smoothTime),
            Mathf.SmoothDamp(current.w, target.w, ref currentVelocity.w, smoothTime)
        ).normalized;
        // compute deriv
        var dtInv = 1f / Time.smoothDeltaTime;
        currentVelocity.x = (result.x - current.x) * dtInv;
        currentVelocity.y = (result.y - current.y) * dtInv;
        currentVelocity.z = (result.z - current.z) * dtInv;
        currentVelocity.w = (result.w - current.w) * dtInv;
        return new Quaternion(result.x, result.y, result.z, result.w);
    }
}

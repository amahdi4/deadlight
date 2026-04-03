using UnityEngine;

namespace Deadlight.Level
{
    /// <summary>
    /// Utility for applying ground-level collision footprints to map objects.
    /// Colliders represent where an object physically blocks movement on the ground plane.
    /// All footprints are derived from the actual sprite bounds so collision always
    /// matches what the player sees.
    /// </summary>
    public static class MapFootprintCollider
    {
        /// <summary>
        /// Computes a footprint collider from the object's SpriteRenderer bounds.
        /// The collider covers the bottom portion of the sprite, matching the
        /// visible base of the object. Works correctly regardless of sprite pivot,
        /// PPU, or object scale.
        /// </summary>
        /// <param name="collider">The BoxCollider2D to configure.</param>
        /// <param name="widthRatio">Fraction of sprite width the collider covers (0-1).</param>
        /// <param name="depthRatio">Fraction of sprite height used as ground depth (0-1).</param>
        /// <param name="minDepth">Minimum collider depth in local units.</param>
        public static void ApplyFromSprite(
            BoxCollider2D collider,
            float widthRatio = 0.9f,
            float depthRatio = 0.4f,
            float minDepth = 0.4f)
        {
            if (collider == null) return;

            var sr = collider.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;

            Bounds b = sr.sprite.bounds;
            float width = b.size.x * widthRatio;
            float depth = Mathf.Max(minDepth, b.size.y * depthRatio);

            // Anchor the collider at the bottom edge of the sprite
            float centerY = b.min.y + depth * 0.5f;

            collider.size = new Vector2(width, depth);
            collider.offset = new Vector2(0f, centerY);
        }

        /// <summary>
        /// Returns the sprite size in local space for registration purposes.
        /// </summary>
        public static Vector2 GetSpriteSize(SpriteRenderer sr)
        {
            if (sr == null || sr.sprite == null) return Vector2.one;
            return sr.sprite.bounds.size;
        }
    }
}

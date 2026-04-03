using System.Collections;
using Deadlight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Deadlight.UI
{
    public class CombatCrosshair : MonoBehaviour
    {
        private RectTransform reticleRect;
        private Image reticleImage;
        private Canvas canvas;

        private Player.PlayerShooting playerShooting;
        private Player.PlayerController playerController;
        private Rigidbody2D playerBody;
        private float pulseAmount;
        private bool isReloading;

        public void Initialize(RectTransform rect, Image image, Canvas rootCanvas)
        {
            reticleRect = rect;
            reticleImage = image;
            canvas = rootCanvas;
            StartCoroutine(BindPlayerRoutine());
        }

        private IEnumerator BindPlayerRoutine()
        {
            while (playerShooting == null)
            {
                BindPlayer();
                yield return new WaitForSeconds(0.2f);
            }
        }

        private void BindPlayer()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                return;
            }

            playerShooting = player.GetComponent<Player.PlayerShooting>();
            playerController = player.GetComponent<Player.PlayerController>();
            playerBody = player.GetComponent<Rigidbody2D>();

            if (playerShooting != null)
            {
                playerShooting.OnWeaponFired -= OnWeaponFired;
                playerShooting.OnReloadStarted -= OnReloadStarted;
                playerShooting.OnReloadCompleted -= OnReloadCompleted;
                playerShooting.OnWeaponFired += OnWeaponFired;
                playerShooting.OnReloadStarted += OnReloadStarted;
                playerShooting.OnReloadCompleted += OnReloadCompleted;
            }
        }

        private void Update()
        {
            if (reticleRect == null || reticleImage == null || canvas == null)
            {
                return;
            }

            bool showCrosshair = GameManager.Instance == null || GameManager.Instance.IsGameplayState;
            reticleImage.enabled = showCrosshair;
            if (!showCrosshair)
            {
                return;
            }

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    Input.mousePosition,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    out Vector2 localPoint))
            {
                reticleRect.anchoredPosition = localPoint;
            }

            float movementSpread = playerBody != null ? playerBody.linearVelocity.magnitude * 2.8f : 0f;
            float weaponSpread = playerShooting?.CurrentWeapon != null ? playerShooting.CurrentWeapon.spread * 0.85f : 3f;
            float reloadSpread = isReloading ? 16f : 0f;
            float targetSize = 34f + Mathf.Clamp(movementSpread + weaponSpread + reloadSpread, 4f, 28f) + pulseAmount;

            reticleRect.sizeDelta = Vector2.Lerp(reticleRect.sizeDelta, new Vector2(targetSize, targetSize), Time.deltaTime * 14f);
            pulseAmount = Mathf.MoveTowards(pulseAmount, 0f, Time.deltaTime * 38f);
            reticleRect.Rotate(Vector3.forward, Time.deltaTime * 12f);

            Color tint = DeadlightUITheme.SoftEdge;
            if (isReloading)
            {
                tint = DeadlightUITheme.Warning;
            }
            else if (IsHoveringEnemy())
            {
                tint = DeadlightUITheme.Danger;
            }

            reticleImage.color = Color.Lerp(reticleImage.color, tint, Time.deltaTime * 18f);
        }

        private bool IsHoveringEnemy()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return false;
            }

            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorld);
            foreach (Collider2D hit in hits)
            {
                if (hit != null && hit.GetComponent<Enemy.EnemyHealth>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnWeaponFired()
        {
            pulseAmount = Mathf.Min(pulseAmount + 12f, 16f);
        }

        private void OnReloadStarted()
        {
            isReloading = true;
            pulseAmount = Mathf.Min(pulseAmount + 6f, 14f);
        }

        private void OnReloadCompleted()
        {
            isReloading = false;
            pulseAmount = Mathf.Min(pulseAmount + 4f, 10f);
        }

        private void OnDestroy()
        {
            if (playerShooting != null)
            {
                playerShooting.OnWeaponFired -= OnWeaponFired;
                playerShooting.OnReloadStarted -= OnReloadStarted;
                playerShooting.OnReloadCompleted -= OnReloadCompleted;
            }
        }
    }
}

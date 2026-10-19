using AbyssHunter.Combat;
using AbyssHunter.Inventory;
using AbyssHunter.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssHunter.Save
{
    public sealed class SaveManager : MonoBehaviour
    {
        private const int CurrentSaveVersion = 1;

        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private ItemDatabase itemDatabase;

        private GameSaveData sessionCheckpoint;

        public bool HasCheckpoint => sessionCheckpoint != null;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                SaveGame();
            }

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                LoadGame();
            }
        }

        public void SaveGame()
        {
            if (FindFirstObjectByType<AbyssHunter.World.AbyssExpedition>() != null) return;
            if (!ReferencesAreValid() || playerHealth.IsDead || MatchResult.HasEnded)
            {
                Debug.LogWarning("玩家已死亡、对局已结束或存档引用不完整，本次未保存。", this);
                return;
            }

            sessionCheckpoint = new GameSaveData
            {
                version = CurrentSaveVersion,
                player = new PlayerSaveData
                {
                    positionX = playerTransform.position.x,
                    positionY = playerTransform.position.y,
                    positionZ = playerTransform.position.z,
                    currentHealth = playerHealth.CurrentHealth
                },
                inventorySlots = playerInventory.CaptureSaveData()
            };

            Debug.Log("已保存本局检查点。按 F9 可以回滚。", this);
        }

        public void LoadGame()
        {
            if (FindFirstObjectByType<AbyssHunter.World.AbyssExpedition>() != null) return;
            if (!ReferencesAreValid())
            {
                Debug.LogWarning("存档引用没有配置完整。", this);
                return;
            }

            // A checkpoint only restores player data, not the completed match or death lifecycle.
            if (playerHealth.IsDead || MatchResult.HasEnded)
            {
                Debug.Log("对局已结束或玩家已死亡，无法回滚检查点。请按 R 重开或按 M 返回菜单。", this);
                return;
            }

            if (sessionCheckpoint == null)
            {
                Debug.Log("本局尚未创建检查点，请先按 F5。", this);
                return;
            }

            RestorePlayer(sessionCheckpoint.player);
            playerInventory.RestoreSaveData(
                sessionCheckpoint.inventorySlots,
                itemDatabase);

            Debug.Log("已回滚到本局检查点。", this);
        }

        private void RestorePlayer(PlayerSaveData playerData)
        {
            if (playerData == null)
            {
                return;
            }

            CharacterController controller =
                playerTransform.GetComponent<CharacterController>();
            bool controllerWasEnabled =
                controller != null && controller.enabled;

            if (controllerWasEnabled)
            {
                controller.enabled = false;
            }

            playerTransform.position = new Vector3(
                playerData.positionX,
                playerData.positionY,
                playerData.positionZ);

            if (controllerWasEnabled)
            {
                controller.enabled = true;
            }

            playerHealth.RestoreHealth(playerData.currentHealth);
        }

        private bool ReferencesAreValid()
        {
            return playerTransform != null &&
                   playerHealth != null &&
                   playerInventory != null &&
                   itemDatabase != null;
        }
    }
}

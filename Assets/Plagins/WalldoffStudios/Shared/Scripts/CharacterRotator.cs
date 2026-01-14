using UnityEngine;

namespace WalldoffStudios.ToonCharacter
{
    public class CharacterRotator : MonoBehaviour
    {
        [SerializeField] private float rotateSpeed = 50f; // Increased to a reasonable value for visible rotation

        private void Update()
        {
            float rotationAmount = rotateSpeed * Time.deltaTime;
            transform.Rotate(0f, rotationAmount, 0f, Space.Self);
        }
    }
}
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BusJam
{
    /// <summary>
    /// Simple View for the bus
    /// </summary>
    public class Bus : MonoBehaviour
    {
        public GameObject passengerHeadPrefab;
        public int busCapacity = 3;

        public ColorId Colour { get; private set; }

        private readonly List<GameObject> passengerHeads = new();
        private int passengerCount = 0;
        public int PassengerCount => passengerCount;

        public bool IsFull => passengerCount >= busCapacity;

        public void SetColour(ColorId id)
        {
            Colour = id;
            var rend = GetComponent<Renderer>();
            if (rend != null) rend.material.color = id.ToUnityColor();
        }

        public void AddPassenger()
        {
            passengerCount++;
            UpdateVisuals(passengerCount);
        }

        public Tween Depart()
        {
            return transform.DOMoveX(transform.position.x + 20f, 1f)
                .OnComplete(() => Destroy(gameObject));
        }

        private void UpdateVisuals(int count)
        {
            // Clear existing heads
            foreach (var head in passengerHeads)
            {
                Destroy(head);
            }

            passengerHeads.Clear();

            // Create new heads and arrange them horizontally
            float spacing = 0.5f;
            float startX = -((count - 1) * spacing) / 2f;
            for (int i = 0; i < count; i++)
            {
                var head = Instantiate(passengerHeadPrefab, transform);
                head.transform.localPosition = new Vector3(startX + i * spacing, 1.5f, -0.5f);
                passengerHeads.Add(head);
            }
        }
    }
}
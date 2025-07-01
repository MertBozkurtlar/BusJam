using UnityEngine;
using System;
using System.Collections.Generic;

namespace BusJam 
{
    /// <summary>
    /// Controls an individual passenger's behavior (movement and interaction). 
    /// Uses events to communicate with managers.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Passenger : MonoBehaviour 
    {
        public int Row { get; private set; }
        public int Col { get; private set; }
        public ColorId Colour { get; private set; }

        private bool _isWaiting = false;
        private GridManager gridManager;  // Reference for coordinate conversion

        // Events triggered on player interaction and on reaching destination
        public static event Action<Passenger> OnPassengerClicked;
        public static event Action<Passenger> OnReachedExitRow;

        /// <summary>Initializes the passenger's data (position, color, and grid reference).</summary>
        public void Init(int row, int col, ColorId colour, GridManager gridMgr) 
        {
            Row = row;
            Col = col;
            Colour = colour;
            gridManager = gridMgr;
            // Set the passenger's visual color
            Renderer rend = GetComponent<Renderer>();
            if (rend != null) 
            {
                rend.material.color = colour.ToUnityColor();
            }
        }

        /// <summary>Marks this passenger as waiting (in the waiting area) so they cannot be clicked again.</summary>
        public void SetWaiting() 
        {
            _isWaiting = true;
        }

        private void OnMouseDown() 
        {
            if (_isWaiting) return;
            // Raise an event instead of directly calling GameController (decoupled input handling)
            OnPassengerClicked?.Invoke(this);
        }

        /// <summary>Starts moving the passenger along a path defined by grid coordinates.</summary>
        public void PlayPath(IReadOnlyList<Vector2Int> path, float speed = 4f) 
        {
            StartCoroutine(PathRoutine(path, speed));
        }

        /// <summary>Moves the passenger towards a single target point in world space.</summary>
        public void MoveToPoint(Vector3 target, float speed = 4f, Action onDone = null) 
        {
            StartCoroutine(MoveToPointRoutine(target, speed, onDone));
        }

        // Coroutine to move along a multi-step grid path
        private System.Collections.IEnumerator PathRoutine(IReadOnlyList<Vector2Int> path, float speed) 
        {
            foreach (var step in path) 
            {
                Vector3 worldPos = gridManager.GridToWorld(step.x, step.y);
                yield return MoveToPointRoutine(worldPos, speed, null);
                // Update internal grid coordinates as the passenger moves
                Row = step.x;
                Col = step.y;
            }
            // Path complete: passenger has reached the exit row
            OnReachedExitRow?.Invoke(this);
        }

        // Coroutine for smooth movement to a single point
        private System.Collections.IEnumerator MoveToPointRoutine(Vector3 target, float speed, Action onDone) 
        {
            while (Vector3.Distance(transform.position, target) > 0.01f) 
            {
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                yield return null;
            }
            onDone?.Invoke();
        }
    }
}

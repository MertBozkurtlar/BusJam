using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual controller for a passenger. It stores its grid coordinates and
/// lets the <see cref="LevelManager"/> know when it is clicked.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Passenger : MonoBehaviour
{
    public int Row { get; private set; }
    public int Col { get; private set; }
    public ColorId Colour { get; private set; }

    private LevelManager manager;

    public void Init(LevelManager mgr, int row, int col, ColorId colour)
    {
        manager = mgr;
        Row = row;
        Col = col;
        Colour = colour;
        GetComponent<Renderer>().material.color = colour.ToUnityColor();
    }

    private void OnMouseDown() => manager.OnPassengerClicked(this);

    public void PlayPath(IReadOnlyList<Vector2Int> path, float speed = 4f)
    {
        StartCoroutine(PathRoutine(path, speed));
    }

    public void MoveToPoint(Vector3 target, float speed = 4f, System.Action onDone = null)
    {
        StartCoroutine(MoveToPointRoutine(target, speed, onDone));
    }

    private IEnumerator PathRoutine(IReadOnlyList<Vector2Int> path, float speed)
    {
        foreach (var step in path)
        {
            Vector3 world = manager.GridToWorld(step.x, step.y);
            yield return MoveToPointRoutine(world, speed, null);
            Row = step.x;
            Col = step.y;
        }
        manager.NotifyPassengerArrived(this);
    }

    private IEnumerator MoveToPointRoutine(Vector3 target, float speed, System.Action onDone)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position =
                Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        onDone?.Invoke();
    }
}
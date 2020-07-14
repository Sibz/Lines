using System;
using System.Collections.Generic;
using Sibz.Lines;
using UnityEngine;

[RequireComponent(typeof(MoveToCursor))]
public class SnapToNode : MonoBehaviour
{
    public           GameObject       Tool;
    private readonly List<GameObject> targets = new List<GameObject>();

    private SnapNotifierBehaviour snapNotifier;
    private MoveToCursor          targetMoveToCursorComponent;
    private MoveToCursor          thisMoveToCursorComponent;

    private void Start()
    {
        targetMoveToCursorComponent       = Tool.GetComponent<MoveToCursor>();
        thisMoveToCursorComponent         = GetComponent<MoveToCursor>();
        thisMoveToCursorComponent.enabled = false;
        snapNotifier                      = Tool.GetComponent<SnapNotifierBehaviour>();
        if (gameObject.layer != 9) throw new Exception("Layer of SnapToNode gameObject must be nodeTools");
    }

    private void OnDisable()
    {
        targets.Clear();
    }

    private void Update()
    {
        if (snapNotifier) snapNotifier.SnappedTo = null;

        if (targets.Count == 0) return;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            Tool.transform.position = transform.position;
            return;
        }

        var        maxLen      = float.MaxValue;
        Vector3    targetPos   = default;
        var        colliderPos = transform.position;
        GameObject snappedTo   = null;
        for (int i = targets.Count - 1; i >= 0; i--)
            if (!targets[i] || !targets[i].activeSelf)
                targets.RemoveAt(i);

        foreach (var target in targets)
        {
            var   pos              = target.transform.position;
            float distanceToTarget = Vector3.Distance(pos, colliderPos);
            if (distanceToTarget > maxLen) continue;

            maxLen    = distanceToTarget;
            targetPos = pos;
            snappedTo = target;
        }

        if (snappedTo) Tool.transform.position = targetPos;

        if (snapNotifier) snapNotifier.SnappedTo = snappedTo;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer != 8 || Input.GetKey(KeyCode.LeftShift)) return;

        if (!targets.Contains(other.gameObject)) targets.Add(other.gameObject);

        targetMoveToCursorComponent.enabled = false;
        thisMoveToCursorComponent.enabled   = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != 8) return;

        if (targets.Contains(other.gameObject)) targets.Remove(other.gameObject);

        targetMoveToCursorComponent.enabled = true;
        thisMoveToCursorComponent.enabled   = false;
        transform.position                  = Tool.transform.position;
    }
}
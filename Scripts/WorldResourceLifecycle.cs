using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldResourceLifecycle : MonoBehaviour
{
    public Vector2 direction = Vector2.zero;
    private Vector2 previousDirection = Vector2.zero;

    public TileMapManager tmm;
    public int rIndex = -1;

    private bool isMoving = false;
    private float speed = 1f;
    private float tileSize = 0.6f - 0.0625f / 2f;
    IEnumerator Start()
    {
        while (rIndex == -1) // Wait until Data loaded.
        {
            yield return null;
        }
        StartCoroutine(AutoMove());
    }
    IEnumerator AutoMove()
    {
        bool canRun = true;
        while (canRun) // Runs forever
        {
            if (!isMoving) // Only move if not already moving
            {
                if (direction != Vector2.zero)
                {
                    yield return StartCoroutine(MoveStep());
                }
                else
                {
                    canRun = false;
                }
            }
            else
            {
                yield return null; // Wait and check again next frame
            }
        }

        tmm.storeResource(transform.position, rIndex, previousDirection);
        StartCoroutine(DecreaseSize());
    }
    IEnumerator MoveStep()
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        Vector2 additionVector = direction * tileSize;
        Vector3 targetPos = startPos + new Vector3(additionVector.x, additionVector.y, 0);
        float elapsedTime = 0f;

        while (elapsedTime < tileSize / speed)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, (elapsedTime * speed) / tileSize);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos; // Snap to exact position
        previousDirection = direction;
        direction = tmm.getDirection(transform.position);
        isMoving = false; // Allow next movement
    }
    IEnumerator DecreaseSize()
    {
        Vector3 startSize = transform.localScale;
        Vector3 targetSize = Vector3.zero;
        float elapsedTime = 0f;
        while (elapsedTime < tileSize / speed)
        {
            transform.localScale = Vector3.Lerp(startSize, targetSize, (elapsedTime * speed) / tileSize);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}

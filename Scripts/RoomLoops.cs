using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomLoops : MonoBehaviour
{
    public GameObject resourceObjPrefab;
    public Transform resourceObjContainer;

    public TileMapManager tmm;

    private SaveLoadDataManager sm;
    private PlayerData playerData;
    IEnumerator Start()
    {
        sm = GameObject.FindGameObjectWithTag("SaveLoadDataManager").GetComponent<SaveLoadDataManager>();
        while (!sm.Initialized)
        {
            yield return null;
        }

        playerData = sm.playerData;
    }

    public IEnumerator ResourceSpawnCoroutine(PixelData pixel)
    {
        while (pixel.rIndex != -1)  // Run only if ShouldRun is true
        {
            float timePerResource = sm.resourceDataDatabase.resources[pixel.rIndex].time;
            if (pixel.index == tmm.dropperIndex) timePerResource *= 2;
            yield return new WaitForSeconds(timePerResource);
            SpawnResource(pixel);
        }
    }
    void SpawnResource(PixelData pixel)
    {
        if (pixel.isMoving) return;

        ResourceData resourceData = sm.resourceDataDatabase.resources[pixel.rIndex];
        if (!AreResourcesSufficient(resourceData, pixel) || playerData.energy.currentEnergy > playerData.energy.maxEnergy)
        {
            pixel.wasInactive = true;
            tmm.SetInactiveTile(pixel);
            return;
        }
        else if (pixel.wasInactive)
        {
            pixel.wasInactive = false;
            tmm.SetAnimatedTile(pixel);
        }

        SubResources(resourceData, pixel);

        playerData.level.xp += resourceData.xp;

        GameObject resourceObj = Instantiate(resourceObjPrefab, tmm.getWorldPositionFromTile(pixel), Quaternion.Euler(0, 0, 0), resourceObjContainer);
        resourceObj.GetComponent<SpriteRenderer>().sprite = sm.resourceImages[resourceData.image_name];
        WorldResourceLifecycle wrl = resourceObj.GetComponent<WorldResourceLifecycle>();
        wrl.direction = getRoomDirection(pixel.rotation);
        wrl.tmm = tmm;
        wrl.rIndex = pixel.rIndex;
    }
    public bool AreResourcesSufficient(ResourceData resourceData, PixelData pixel)
    {
        if (pixel.index == tmm.dropperIndex)
        {
            if (playerData.resources.resources[resourceData.index].quantity >= 1)
                return true;
            return false;
        }
        foreach (ResourceRequirement res1 in resourceData.resource_requirements)
        {
            ResourceRequirement res2 = pixel.storedResources.Find(r => r.index == res1.index);

            if (res2 == null || res2.quantity < res1.quantity)
            {
                return false;
            }
        }

        return true; // All resources in storedResources1 are satisfied
    }
    public void SubResources(ResourceData resourceData, PixelData pixel)
    {
        if (pixel.index == tmm.dropperIndex)
        {
            playerData.resources.resources[resourceData.index].quantity -= 1;
            return;
        }
        foreach (ResourceRequirement res1 in resourceData.resource_requirements)
        {
            ResourceRequirement res2 = pixel.storedResources.Find(r => r.index == res1.index);
            res2.quantity -= res1.quantity;
        }
    }
    Vector2 getRoomDirection(float rotation)
    {
        switch (rotation)
        {
            case 0f:
                return Vector2.up;
            case 90f:
                return Vector2.left;
            case 180f:
                return Vector2.down;
            case 270f:
                return Vector2.right;
            default:
                print("Rotation is not exist: " + rotation);
                return Vector2.zero;
        }
    }
}

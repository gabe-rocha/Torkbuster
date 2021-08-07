using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GameObjectPooler : MonoBehaviour
{
    protected List<GameObject> _listOfReadyObjects = new List<GameObject>();
    protected List<GameObject> _listOfUsedObjects = new List<GameObject>();

    [SerializeField] protected int _amountToHold;
    [SerializeField] protected bool _expandable;

    /// <summary>
    /// Every time we need to expand, how many more items do we instantiate?
    /// </summary>
    [SerializeField] protected int _expansionAmountPerNeed;
    [SerializeField] protected GameObject _objectToBePooled;

    private void OnEnable()
    {
        for (int i = 0; i < _amountToHold; i++)
        {
            GameObject go = Instantiate(_objectToBePooled, transform.position, _objectToBePooled.transform.rotation);
            go.SetActive(false);
            go.transform.parent = gameObject.transform;
            _listOfReadyObjects.Add(go);
        }
    }

    public GameObject GetAPooledObject()
    {
        if (_listOfReadyObjects.Count > 0)
        {
            GameObject go = _listOfReadyObjects[0];
            go.SetActive(true);

            _listOfReadyObjects.RemoveAt(0);
            _listOfUsedObjects.Add(go);

            return go;
        }
        else
        {
            if (_expandable)
            {
                //create more
                for (int i = 0; i < _expansionAmountPerNeed; i++)
                {
                    GameObject go = Instantiate(_objectToBePooled, transform.position, _objectToBePooled.transform.rotation);
                    go.SetActive(false);
                    go.transform.parent = gameObject.transform;
                    _listOfReadyObjects.Add(go);
                }
                return GetAPooledObject();
            }
            else
            {
                return null;
            }
        }
    }

    public void ReturnUsedObject(GameObject usedGO)
    {
        _listOfUsedObjects.Remove(usedGO);
        usedGO.SetActive(false);
        _listOfReadyObjects.Add(usedGO);
    }
}
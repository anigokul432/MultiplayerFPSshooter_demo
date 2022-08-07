using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    public GameObject classMenuRoot;

    PhotonView PV;
    Transform Spawns;
    Transform spawnPos;

    public GameObject ClassMenu;
    Button TrooperButton, RunnerButton;

    void Awake()
    {
        PV = GetComponent<PhotonView>();
        Instance = this;
        Spawns = GameObject.FindGameObjectWithTag("Spawn").transform;
        spawnPos = Spawns.GetChild(Random.Range(0, Spawns.childCount));

    }

    private void Start()
    {
        ClassMenu = Instantiate(classMenuRoot, Vector3.zero, Quaternion.identity, transform);
        ClassMenu.SetActive(PV.IsMine);

        TrooperButton = transform.GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponentInChildren<Button>();
        RunnerButton = transform.GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetComponentInChildren<Button>();

        TrooperButton.onClick.AddListener(CreateTrooper);
        RunnerButton.onClick.AddListener(CreateRunner);

        Spawn();
    }

    public void Spawn()
    {      
        if (PV.IsMine)
        {
            Debug.Log("SPAWN");
            ClassMenu.transform.GetChild(0).gameObject.SetActive(true);
        }
        else
        {

        }
    }

    public void CreateTrooper()
    {
        Debug.Log("Instantiated Trooper");
        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Trooper"), spawnPos.position, spawnPos.rotation);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ClassMenu.transform.GetChild(0).gameObject.SetActive(false);
    }

    public void CreateRunner()
    {
        Debug.Log("Instantiated Runner");
        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Runner"), spawnPos.position, spawnPos.rotation);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ClassMenu.transform.GetChild(0).gameObject.SetActive(false);
    }

    public void CreateDeadBody(string _class, Vector3 position, Quaternion rotation)
    {
        GameObject deadBody;
        deadBody = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "dead" + _class), position + Vector3.down, rotation);
        deadBody.transform.GetChild(1).GetComponent<Rigidbody>().AddForce(Vector3.down - Weapon.Instance.hitDir * 100, ForceMode.Impulse);
    }


    private void Update()
    {
        if (Input.GetKeyDown("c"))
        {
            ClassMenu.transform.GetChild(0).gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}

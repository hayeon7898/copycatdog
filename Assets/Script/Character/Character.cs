using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

public class Character : MonoBehaviour, IWallBoom
{
    public enum Direction { Left, Up, Right, Down }; // 0 = 왼쪽, 1 = 위, 2 = 오른쪽, 3 = 아래,
    [SerializeField]
    private GameObject waterBalloonPrefab;
    [SerializeField]
    private FrontChecker frontChecker;
    private GameObject frontCheckerObject;
    public int waterBalloonPower;
    public int waterBalloonMaxCount = 1;
    public int currentWaterBalloons = 0;
    public float moveSpeed = 5f;
    public float inWaterSpeed = 1f; // 물풍선에 갇혔을때 속도
    private bool isTrapped = false;
    private Waterballoon tempWaterBalloon;
    private Vector3 waterBalloonPos;
    private Vector3 moveDirect;
    private float preMoveSpeed;
    public bool canPush = false;
    public bool canThrow = false;
    public Direction playerDir = Direction.Left;
    public float pushTime = 0;

    public KeyCode[] playerKey; // 0 = 왼쪽, 1 = 위, 2 = 오른쪽, 3 = 아래, 4 = 물풍선설치, 5 = 아이템 사용

    private Animator animator;


    //아이템 유무와 관련된 변수들    
    int countNeedleItem = 0;//바늘 아이템 개수
    public bool isShieldItem = false;//방패 아이템 유무 여부
    public bool isTurtleItem = false; //거북이 아이템 유무 여부
    public bool isUfoItem = false; //Ufo 아이템 유무 여부
    public bool isOwlItem = false; //부엉이 아이템 유무 여부

    //바늘 아이템 사용 여부와 관련된 변수
    public bool canEscape = false;

    //방패 보호 상태와 관련된 변수들
    private bool isShieldProtected = false;
    private float shieldProtectionTime = 5f;
    private float shieldProtectionTimer = 0f;

    //WaterBalloonBoom 함수와 관련된 변수들
    private float needleItemDelay = 5f;
    private float deathDelay = 10f;
    private float timer = 0f;

    //탑승 아이템 관련 변수들
    public bool isRidingItem = false;
    private IRideable currentRideable; // 현재 탑승 중인 아이템


    void Start()
    {
        frontCheckerObject = frontChecker.gameObject;
        preMoveSpeed = moveSpeed;
        animator = this.GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        
        frontCheckerObject.transform.rotation = Quaternion.Euler(0,0,-90 * (int)playerDir);
        if (isTrapped)
        {
            transform.Translate(moveDirect / moveSpeed * Time.deltaTime * inWaterSpeed);
            timer += Time.deltaTime;
            if(timer >= 7f)
            {
                animator.SetBool("Die", true);
                GameOver();
            }
        }
        else
        {
            transform.Translate(moveDirect * Time.deltaTime);
        }
    }

    void Update()
    {
        if (preMoveSpeed != moveSpeed)
        {
            moveDirect = moveDirect.normalized * moveSpeed;
        }
        preMoveSpeed = moveSpeed;

        if (Input.GetKeyDown(playerKey[4]))
        {
            SpawnWaterBalloon();
        }

        if (Input.GetKeyDown(playerKey[0]))
        {
            moveDirect = Vector3.left * moveSpeed;
            playerDir = Direction.Left;
            editAnimator(true);
        }
        else if (Input.GetKeyDown(playerKey[2]))
        {
            moveDirect = Vector3.right * moveSpeed;
            playerDir = Direction.Right;
            editAnimator(true);
        }
        else if (Input.GetKeyDown(playerKey[1]))
        {
            moveDirect = Vector3.up * moveSpeed;
            playerDir = Direction.Up;
            editAnimator(true);
        }
        else if (Input.GetKeyDown(playerKey[3]))
        {
            moveDirect = Vector3.down * moveSpeed;
            playerDir = Direction.Down;
            editAnimator(true);
        }

        if (Input.GetKeyUp(playerKey[0]) && moveDirect == Vector3.left * moveSpeed)
        {
            moveDirect = Vector3.zero;
            KeyPushCheck();
        }
        else if (Input.GetKeyUp(playerKey[2]) && moveDirect == Vector3.right * moveSpeed)
        {
            moveDirect = Vector3.zero;
            KeyPushCheck();
        }
        else if (Input.GetKeyUp(playerKey[1]) && moveDirect == Vector3.up * moveSpeed)
        {
            moveDirect = Vector3.zero;
            KeyPushCheck();
        }
        else if (Input.GetKeyUp(playerKey[3]) && moveDirect == Vector3.down * moveSpeed)
        {
            moveDirect = Vector3.zero;
            KeyPushCheck();
        }


  
        if (Input.GetKeyDown(KeyCode.X) && countNeedleItem > 0)
        {
            UseNeedleItem();
        }
    }

    void OnTriggerEnter2D(Collider2D obj)
    {
        
        if (obj.tag == "Attack")
        {
            Debug.Log("attack");
            WaterBalloonBoom();
        }
    }

    void OnTriggerExit2D(Collider2D obj)
    {

        if (obj.tag == "Waterballoon")
        {
            if (obj.gameObject != frontChecker.FrontWaterBalloon)
            {
                return;
            }
        }
    }

    private void OnCollisionStay2D(Collision2D obj)
    {
        if (obj.gameObject.tag == "WaterBalloon" && canPush)
        {
            if (obj.gameObject != frontChecker.FrontWaterBalloon || moveDirect == Vector3.zero)
            {
                pushTime = 0;
                return;
            }
            pushTime += Time.deltaTime;
            if (pushTime > .25f)
            {
                pushTime = 0;
                PushWaterBalloon();
            }
        }
    }

    private void OnCollisionExit2D(Collision2D obj)
    {
        if (obj.gameObject.tag == "WaterBalloon")
        {
            pushTime = 0;
        }
    }


    void SpawnWaterBalloon()
    {

        waterBalloonPos = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0);
        if (Map.instance.mapArr[7 - (int)waterBalloonPos.y, (int)waterBalloonPos.x + 7] == 3)
        {
            if (canThrow)
            {
                Destroy(tempWaterBalloon.gameObject);
                Map.instance.mapArr[7 - (int)waterBalloonPos.y, (int)waterBalloonPos.x + 7] = 0;
                ThrowWaterBalloon();
            }
            return;
        }
        if (currentWaterBalloons >= waterBalloonMaxCount)
        {
            return;
        }
        currentWaterBalloons++;
        tempWaterBalloon = Instantiate(waterBalloonPrefab, waterBalloonPos, Quaternion.identity).GetComponent<Waterballoon>();
        tempWaterBalloon.Power = waterBalloonPower;
        tempWaterBalloon.Player = this;
        tempWaterBalloon.Position = new int[2] { (int)waterBalloonPos.x + 7, 7 - (int)waterBalloonPos.y };
        Map.instance.mapArr[7 - (int)waterBalloonPos.y, (int)waterBalloonPos.x + 7] = 3;
        tempWaterBalloon.GetComponent<SpriteRenderer>().sortingOrder = 8 - (int)waterBalloonPos.y;
    }

    public void WaterBalloonExploded()
    {
        currentWaterBalloons--;
    }


    //캐릭터가 바늘 아이템 획득하는 함수
    public void EquipNeedleItem()
    {
        countNeedleItem++;

    }

    //바늘 아이템 사용하는 함수
    void UseNeedleItem()
    {
        countNeedleItem--;
        canEscape = true;
    }

    //캐릭터에 방패 아이템 효과 적용 함수
    public void ApplyShieldItemEffects()
    {
        isShieldItem = true;

    }


    //캐릭터에 거북이 아이템 효과 적용 함수
    public void ApplyTurtleItemEffects(TurtleSpeed speed)
    {

        isTurtleItem = true;

        if (speed == TurtleSpeed.Fast)
        {
            moveSpeed = 8f;
        }

        else
        {
            moveSpeed = 4f;
        }
    }

    //캐릭터에 Ufo 아이템 효과 적용 함수
    public void ApplyUfoItemEffects()
    {
        isUfoItem = true;

    }

    //캐릭터에 부엉이 아이템 효과 적용 함수
    public void ApplyOwlItemEffects()
    {
        isOwlItem = true;
    }

    // 탑승 아이템을 획득하면 호출되는 함수
    public void ApplyRideableItem(IRideable rideableItem)
    {
        if (currentRideable != null)
        {
            Destroy(currentRideable.gameObject);
        }

        currentRideable = rideableItem;
        currentRideable.Ride(this);

        // currentRideable = Instantiate(rideableItem.gameObject, transform.position, Quaternion.identity, transform).GetComponent<IRideable>();
    }


    //캐릭터가 물풍선에 맞은 경우를 구현한 함수

    public void WaterBalloonBoom()
    {

        //거북이를 탄 경우
        if (isTurtleItem)
        {
            isTurtleItem = false;
            //거북이 사라짐
            return;
        }
        //방패 아이템이 켜져있는 경우
        else if (isShieldItem)
        {
            //방패를 보호 상태로 변경하고 타이머 시작
            isShieldProtected = true;
            shieldProtectionTimer = 0f;

            //5초가 지나면 보호 상태가 아님
            shieldProtectionTimer += Time.deltaTime;
            if (shieldProtectionTimer >= shieldProtectionTime)
            {
                isShieldProtected = false;
            }

            //방패 아이템 삭제
            isShieldItem = false;
        }
        //그 외의 경우에는 물풍선에 갇힘
        else
        {
            isTrapped = true;
            animator.SetBool("InWater",true);
            //바늘 아이템이 있는 경우 -> 바늘아이템을 사용했을때로 이전
            /*
            if (countNeedleItem > 0 && timer >= needleItemDelay && canEscape)
            {
                Debug.Log("바늘 아이템을 이용해 물풍선 탈출!");

                //물풍선을 탈출함
                isTrapped = false;
                timer = 0f;
                canEscape = false;
            }
            */
        }



    }

    public void KeyPushCheck()
    {
        if (Input.GetKey(playerKey[0]))
        {
            moveDirect = Vector3.left * moveSpeed;
            playerDir = Direction.Left;
        }
        else if (Input.GetKey(playerKey[2]))
        {
            moveDirect = Vector3.right * moveSpeed;
            playerDir = Direction.Right;
        }
        else if (Input.GetKey(playerKey[1]))
        {
            moveDirect = Vector3.up * moveSpeed;
            playerDir = Direction.Up;
        }
        else if (Input.GetKey(playerKey[3]))
        {
            moveDirect = Vector3.down * moveSpeed;
            playerDir = Direction.Down;
        }
        else
        {
            editAnimator(false);
            return;
        }
        editAnimator(true);
    }

    public void PushWaterBalloon()
    {
        waterBalloonPos = new Vector3(Mathf.Round(frontChecker.FrontWaterBalloon.transform.position.x), Mathf.Round(frontChecker.FrontWaterBalloon.transform.position.y), 0);
        int x = 7 - (int)waterBalloonPos.y;
        int y = (int)waterBalloonPos.x + 7;
        Map.instance.mapArr[x, y] = 0;
        if (playerDir == Direction.Left)
        {
            if (y - 1 < 0)
            {
                return;//밀수 없는 경우
            }
            if (Map.instance.mapArr[x, y - 1] != 0)
            {
                return;//밀수 없는 경우
            }
            for (int i = 2; ; i++)//왼
            {
                if (y - i < 0) // 맵 밖 14 = -1
                {
                    tempWaterBalloon = frontChecker.FrontWaterBalloon.GetComponent<Waterballoon>();
                    tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + (y - i + 1), 7 - x));
                    break;
                }
                else if (Map.instance.mapArr[x, y - i] != 0) // 장애물을 만남
                {

                    tempWaterBalloon = frontChecker.FrontWaterBalloon.GetComponent<Waterballoon>();
                    tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + (y - i + 1), 7 - x));
                    break;
                }
            }
        }
        else if (playerDir == Direction.Up)
        {
            if (x - 1 < 0)
            {
                return;
            }
            if (Map.instance.mapArr[x - 1, y] != 0)
            {
                return;
            }
            for (int i = 2; ; i++)//위
            {
                if (x - i < 0) // 맵 밖
                {
                    tempWaterBalloon = frontChecker.FrontWaterBalloon.GetComponent<Waterballoon>();
                    tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + y, 7 - (x - i + 1)));
                    break;
                }
                else if (Map.instance.mapArr[x - i, y] != 0) 
                {
                    tempWaterBalloon = frontChecker.FrontWaterBalloon.GetComponent<Waterballoon>();
                    tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + y, 7 - (x - i + 1)));
                    break;
                }
            }
        }
        else if (playerDir == Direction.Down)
        {
            if (x + 1 >= 15)
            {
                return;
            }
            if (Map.instance.mapArr[x + 1, y] != 0)
            {
                return;
            }
            for (int i = 2; ; i++)//아
            {
                if (x + i >= 15) // 맵 밖
                {
                    tempWaterBalloon = frontChecker.FrontWaterBalloon.GetComponent<Waterballoon>();
                    tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + y, 7 - (x + i - 1)));
                    break;
                }
                else if (Map.instance.mapArr[x + i, y] != 0) 
                {
                    tempWaterBalloon = frontChecker.FrontWaterBalloon.GetComponent<Waterballoon>();
                    tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + y, 7 - (x + i - 1)));
                    break;
                }
            }
        }
        else if (playerDir == Direction.Right)
        {
            if (y + 1 >= 15)
            {
                return;
            }
            if (Map.instance.mapArr[x, y + 1] != 0)
            {
                return;
            }
            for (int i = 2; ; i++)// 오
            {
                if (y + i >= 15) // 맵 밖
                {
                    tempWaterBalloon = frontChecker.FrontWaterBalloon.GetComponent<Waterballoon>();
                    tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + (y + i - 1), 7 - x));
                    break;
                }
                else if (Map.instance.mapArr[x, y + i] != 0) 
                {
                    tempWaterBalloon = frontChecker.FrontWaterBalloon.GetComponent<Waterballoon>();
                    tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + (y + i - 1), 7 - x));
                    break;
                }
            }
        }
        tempWaterBalloon.Power = waterBalloonPower;
        tempWaterBalloon.Player = this;
    }

    public void ThrowWaterBalloon()
    {
        waterBalloonPos = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0);
        int x = 7 - (int)Mathf.Round(transform.position.y);
        int y = (int)Mathf.Round(transform.position.x) + 7;
        if (playerDir == Direction.Left)
        {
            for (int i = 7; ; i++)//왼
            {
                if (y - i < 0) // 맵 밖 14 = -1
                {
                    if (Map.instance.mapArr[x, y - i + 15] == 0)
                    {
                        tempWaterBalloon = Instantiate(waterBalloonPrefab, waterBalloonPos, Quaternion.identity).GetComponent<Waterballoon>();
                        tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + (y - i + 15), 7 - x));
                        break;
                    }
                }
                else if (Map.instance.mapArr[x, y - i] == 0) // 장애물을 만남
                {
                    
                    tempWaterBalloon = Instantiate(waterBalloonPrefab, waterBalloonPos, Quaternion.identity).GetComponent<Waterballoon>();
                    tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + (y - i), 7 - x));
                    break;
                }
            }
        }
        else if (playerDir == Direction.Up)
        {
            for (int i = 7; ; i++)//위
            {
                if (x - i < 0) // 맵 밖
                {
                    if (Map.instance.mapArr[x - i + 15, y] == 0)
                    {
                        tempWaterBalloon = Instantiate(waterBalloonPrefab, waterBalloonPos, Quaternion.identity).GetComponent<Waterballoon>();
                        tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + y, 7 - (x - i + 15)));
                        break;
                    }
                }
                else if (Map.instance.mapArr[x - i, y] == 0) // 장애물 만났을때 안부숴지는 벽
                {
                    tempWaterBalloon = Instantiate(waterBalloonPrefab, waterBalloonPos, Quaternion.identity).GetComponent<Waterballoon>();
                    tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + y, 7 - (x - i)));
                    break;
                }
            }
        }
        else if (playerDir == Direction.Down)
        {
            for (int i = 7; ; i++)//아
            {
                if (x + i >= 15) // 맵 밖
                {
                    if (Map.instance.mapArr[x + i - 15, y] == 0)
                    {
                        tempWaterBalloon = Instantiate(waterBalloonPrefab, waterBalloonPos, Quaternion.identity).GetComponent<Waterballoon>();
                        tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + y, 7 - (x + i - 15)));
                        break;
                    }
                }
                else if (Map.instance.mapArr[x + i, y] == 0) // 장애물 만났을때 안부숴지는 벽
                { 
                    tempWaterBalloon = Instantiate(waterBalloonPrefab, waterBalloonPos, Quaternion.identity).GetComponent<Waterballoon>();
                    tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + y, 7 - (x + i)));
                    break;
                }
            }
        }
        else if (playerDir == Direction.Right)
        {
            for (int i = 7; ; i++)// 오
            {
                if (y + i >= 15) // 맵 밖
                {
                    if (Map.instance.mapArr[x, y+i-15] == 0)
                    {
                        tempWaterBalloon = Instantiate(waterBalloonPrefab, waterBalloonPos, Quaternion.identity).GetComponent<Waterballoon>();
                        tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + (y + i - 15), 7 - x));
                        break;
                    }
                }
                else if (Map.instance.mapArr[x, y + i] == 0) // 장애물 만났을때 안부숴지는 벽
                {
                    tempWaterBalloon = Instantiate(waterBalloonPrefab, waterBalloonPos, Quaternion.identity).GetComponent<Waterballoon>();
                    tempWaterBalloon.Move((int)playerDir, new Vector3(-7 + (y + i), 7 - x));
                    break;
                }
            }
        }
        tempWaterBalloon.Power = waterBalloonPower;
        tempWaterBalloon.Player = this;
    }

    private void editAnimator(bool move)
    {
        animator.SetInteger("Direction", (int)playerDir);
        animator.SetBool("Moving",move);
    }

    private void GameOver()
    {
        //게임 오버시 호출 
    }
}

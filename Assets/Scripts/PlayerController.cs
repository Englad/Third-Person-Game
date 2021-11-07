using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;


[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float playerSpeed = 2.0f;
    [SerializeField]
    private float jumpHeight = 1.0f;
    [SerializeField]
    private float gravityValue = -9.81f;
    [SerializeField]
    private float rotationSpeed = 5f;
    [SerializeField]
    private float bulletMaxDistance = 25f;

    [SerializeField]
    private float animationPlayTransition = 0.15f;

    [SerializeField]
    private GameObject bulletPrefab;
    [SerializeField]
    private Transform barrelTransform;
    [SerializeField]
    private Transform bulletParent;
    [SerializeField]
    private float animationSmoothTime = 0.1f;
    [SerializeField]
    private Transform aimTarget;
    [SerializeField]
    private float aimDistance = 15f;

    private CharacterController controller;
    private PlayerInput playerInput;
    private Transform cameraTransform;
    private Vector3 playerVelocity;
    private bool groundedPlayer;

    public GameObject armRig;
    public Rig rig;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction shootAction;
    private InputAction equipPistolAction;

    private Animator animator;
    [SerializeField] 
    private GameObject weapon;

    int pistolJumpAnimation;
    int jumpAnimation;
    int moveXAnimatorParamaterId;
    int moveZAnimatorParamaterId;
    int recoilAnimation;
    int reloadPistol;
    int drawPistol;
    private int shotsFired = 0;
    private bool weaponEquipped = false;
    private bool reloading;

    Vector2 currentAnimationBlendVector;
    Vector2 animationVelocity;

    



    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();
        rig = armRig.GetComponent<Rig>();

        cameraTransform = Camera.main.transform;
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        shootAction = playerInput.actions["Shoot"];
        equipPistolAction = playerInput.actions["EquipPistol"];
        moveXAnimatorParamaterId = Animator.StringToHash("MoveX");
        moveZAnimatorParamaterId = Animator.StringToHash("MoveZ");
        pistolJumpAnimation = Animator.StringToHash("PistolJump");
        jumpAnimation = Animator.StringToHash("Jump");
        recoilAnimation = Animator.StringToHash("RecoilShooting");
        reloadPistol = Animator.StringToHash("ReloadPistol");
        drawPistol = Animator.StringToHash("EquipPistol");
        
    }

    private void OnEnable() {
        shootAction.performed += _ => ShootGun();
        equipPistolAction.performed += _ => ManagePistol();
    }

    private void OnDisable() {
        shootAction.performed -= _ => ShootGun();
        equipPistolAction.performed -= _ => ManagePistol();
    }

    private void ManagePistol() {
        if (!weaponEquipped) {
            weaponEquipped = !weaponEquipped;
            animator.CrossFade(drawPistol, animationPlayTransition);
            weapon.SetActive(true);
            animator.SetBool("hasPistolEquipped", true);
        }
        else {
            weaponEquipped = !weaponEquipped;
            animator.CrossFade(drawPistol, animationPlayTransition);
            weapon.SetActive(false);
            animator.SetBool("hasPistolEquipped", false);
        }

    }

    private void ShootGun() {
        animator.SetBool("shooting", true);
        if (weaponEquipped && !reloading) {
        RaycastHit hit;
        GameObject bullet = GameObject.Instantiate(bulletPrefab, barrelTransform.position, Quaternion.identity, bulletParent);
        BulletController bulletController = bullet.GetComponent<BulletController>();
            if(Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, Mathf.Infinity)) {
                bulletController.target = hit.point;
                bulletController.hit = true;
            }
        else {
                bulletController.target = cameraTransform.position + cameraTransform.forward * bulletMaxDistance;
                bulletController.hit = false;
        }
        if (shotsFired < 5) {
        animator.CrossFade(recoilAnimation, animationPlayTransition);
        shotsFired++;
        }
        else {
            reloading = true;
            animator.CrossFade(reloadPistol, animationPlayTransition);
            shotsFired = 0;
            reloading = false;
        }
    }
    else {
        return;
        }
    animator.SetBool("shooting", false);
    }

    void Update()
    {
        aimTarget.position = cameraTransform.position + cameraTransform.forward * aimDistance;


        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        currentAnimationBlendVector = Vector2.SmoothDamp(currentAnimationBlendVector, input, ref animationVelocity, animationSmoothTime);

        Vector3 move = new Vector3(currentAnimationBlendVector.x, 0, currentAnimationBlendVector.y);
        move = move.x * cameraTransform.right.normalized + move.z * cameraTransform.forward.normalized;
        move.y = 0;
        controller.Move(move * Time.deltaTime * playerSpeed);

        animator.SetFloat(moveXAnimatorParamaterId, currentAnimationBlendVector.x);
        animator.SetFloat(moveZAnimatorParamaterId, currentAnimationBlendVector.y);


        // Changes the height position of the player..
        if (jumpAction.triggered && groundedPlayer)
        {
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
            if (weaponEquipped) {
            animator.CrossFade(pistolJumpAnimation, animationPlayTransition); 
            }
            else {
                animator.CrossFade(jumpAnimation, animationPlayTransition);
            }
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        // rotate towards cam direction
        float targetAngle = cameraTransform.eulerAngles.y; // return y rotation
        Quaternion targetRotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
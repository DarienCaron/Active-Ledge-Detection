using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Made in unity3D
// Credit Darien Caron for use of this code


/*
 *  Creators: Darien
 *  
 *  Description: Type Generic Item holder class that inherits from monobehaviour
 *  
 *  Last Edit: 30 / 11 / 2018 (Darien, Added description)
 * 
 */
public class CreatureLedgeDetection : MonoBehaviour {
    [Tooltip("Max height the ledge can be before we grab it.")]
    public float MaxMantleHeight = .65f;

    [Header("RayCast info")]
    [Tooltip("Length of ray shooting forward to detect walls")]
    public float ChestRayLength = .65f;


    [Tooltip("Percentage cap for dot product when top surface is checked")]
    public float AngleLimitPercentage = -0.8f;

    [Tooltip("Size of the box that gets casted down when checking for a valid grab point")]
    public Vector3 SearchBoxSize = new Vector3(.15f, .25f, .15f);

    public float RayHeightDifference;

    [Header("Layer Mask")]
    [Tooltip("Layer mask for collisions to ignore.")]
    public LayerMask IgnoreCollisionMask;



    public bool IsGrappling = false;


    public Vector3 MantleHitPoint;


    public bool IsLedgeAvailable;
    public bool IsMantleAvailable;


    public bool DebugNoGrapple = false;

  



    [Header("Recovery Time")]
    public float RecoveryTimerMax = 0.75f;


    const float LEDGEGRABZOFFSET = 0.45f;
    const float LEDGEGRABYOFFSET = 0.25f;


    /*
 *  Creators: Darien
 *  
 *  
 *  Desc: Sets the player reference
 *  
 *  Output: None
 *  
 *  Last Edit: 30 / 11 / 2018 (Darien, Description)
 */
    private void Awake()
    {
        m_PlayerRef = GetComponent<PlayerCreature>(); // Initialize our player Reference
        m_ThirdPersonCamera = Camera.main.GetComponent<ThirdPersonCamera>(); // Get our camera reference from the camera class.
        if(m_ThirdPersonCamera)
        {
            m_ThirdPersonCamera.CreateTypeAndAddToDictionary(CameraBehaviourType.CAMBEHAVIOUR_LEDGE, new LedgeCameraBehaviour()); // Add the ledge camera behaviour to our camera behaviour dictionary
        }
        

    }
    /*
 *  Creators: Darien
 *  
 *  
 *  Desc: Checks if a wall has been found. If true, find a potential point to grab on to.
 *  
 *  
 *  Last Edit: 7 / 11 / 2018 (Darien, Description)
 */
    private void Update()
    {
        m_ChestPosition = transform.position + new Vector3(0, transform.lossyScale.y / 2f + RayHeightDifference, 0); // Set our chest position.


        if (!IsGrappling && !m_PlayerRef.B_IsScurrying) // If the player isn't currently grappling something and not using the scurry ability.
        {
            if (FindValidWall()) // Check if theres a valid wall.
            {
                Vector3 CollidedPoint = m_wallHit;

                m_HeightOffset = Vector3.Distance(new Vector3(0, CollidedPoint.y, 0), new Vector3(0, CollidedPoint.y + MaxMantleHeight, 0)); // Calculate the height offset so the player grabs the ledge properly.
                Vector3 Grapple = FindGrapplePoint(CollidedPoint, m_HeightOffset); // Find a point to grapple to.

                if (DebugNoGrapple == false)
                {
                    if (IsLedgeAvailable) // If we found a valid wall and the ledge is available.
                    {
                        if (Grapple != Vector3.zero && !m_PlayerRef.B_IsGrounded) // If the player is airborn.
                        {
                            if (m_CanGrapple) // A final check to see if the players able to grapple
                            {
                                Debug.Log("CanGrapple: " + m_CanGrapple);
                                PlayerGrabLedge(Grapple); // Grab the ledge
                                //Debug.Log("I am able to grab");
                                IsLedgeAvailable = false; // No ledge is available.
                            }
                            
                        }
                    }
                }
            }
            else
            {
                m_wallHit = Vector3.zero; // Otherwise reset the points.
                MantleHitPoint = Vector3.zero;
            }
        }

      


    }

    private void FixedUpdate()
    {
        if (m_PlayerRef.B_DidJump && IsGrappling) // If the player is currently  holding a ledge and they jump.
        {
            m_PlayerRef.SetStopFloorCheck(false); // Stop the players floor check.
            PlayerBrain.s_MasterBrainRef.IsInputLocked = false; 
            transform.position += new Vector3(0, transform.lossyScale.y / 2, transform.forward.z); // Hardcoded mantle value.

            m_PlayerRef.GetComponent<Rigidbody>().isKinematic = false; // Allow other calculations to kick in       
            //Debug.Log("LedgeGRabbing");
       
            m_ThirdPersonCamera.SetCameraBehaviour(CameraBehaviourType.CAMBEHAVIOUR_FOLLOW); // Set our camera behaviour to our follow camera.
            m_Recovering = true;
            IsGrappling = false;
        }

        if(m_Recovering)
        {
            m_RecoveryTimer += Time.fixedDeltaTime; // Recovery timer to ensure the player has time to not hit the ledge with the raycast.
            if(m_RecoveryTimer >= RecoveryTimerMax)
            {
                m_Recovering = false;
                m_CanGrapple = true;
                m_RecoveryTimer = 0;

            }
        }
        
    }


    void PlayerGrabLedge(Vector3 LedgeGrabPosition)
    {        
       if (!IsGrappling) // If the player isn't grappling
       {
           IsGrappling = true; // Set grappling to true.
           m_PlayerRef.B_IsGrounded = false; // The player is no longer grounded.
           m_PlayerRef.GetComponent<Rigidbody>().isKinematic = true; // We want to handle what happens to the player.

            m_CanGrapple = false;
            if (transform.position != LedgeGrabPosition && LedgeGrabPosition != Vector3.zero) // Check if our position is not the ledge grab one, this is for optimization
           {
                LedgeGrabPosition.y -= LEDGEGRABYOFFSET; // Offset the y to fit the animation
                

              
               Vector3 LedgeForward = transform.forward; // Set our ledge grab forward

               
                LedgeGrabPosition.z -= LedgeForward.normalized.z * LEDGEGRABZOFFSET; // Make a constant


                transform.position = LedgeGrabPosition; // Set the players position to the ledge grab point
                transform.rotation = Quaternion.LookRotation(LedgeForward, transform.up); //Rotate to face the ledge
                //PlayerBrain.s_MasterBrainRef.IsInputLocked = true;
               m_PlayerRef.B_IsGrounded = false;
               //Debug.Log("LedgeGRabbing");
               m_PlayerRef.SetStopFloorCheck(true);
                if (PlayerBrain.s_MasterBrainRef.p_PlayerRef.gameObject == m_PlayerRef.gameObject) // If the player body is the player reference
                {

                    
                    m_ThirdPersonCamera.SetCameraBehaviour(CameraBehaviourType.CAMBEHAVIOUR_LEDGE);
                    m_ThirdPersonCamera.SetCameraTarget(PlayerBrain.s_MasterBrainRef.p_PlayerRef.gameObject);

                    m_ThirdPersonCamera.SetCameraPuppeteer(PlayerBrain.s_MasterBrainRef.ControlledPuppeteer);
                    m_ThirdPersonCamera.SetCameraBehaviour(CameraBehaviourType.CAMBEHAVIOUR_LEDGE);

                    // Set the camera to be the ledge grabbing behaviour and set its parameters.
                }

            }

           
       }        

    }
    /*
*  Creators: Darien
*  
*Input: 
*  
*Desc: Checks the raycast forward and verifies if the collided point is indeed a wall. 
*  
*  
*Output: bool isWallfound
*  
*  Last Edit: 16 / 11 / 2018 (Darien, added function)
*/
    protected virtual bool FindValidWall()
    {
        RaycastHit hit;
        if (!m_PlayerRef.GetComponent<CreatureMantleCheck>().GetIsMantling()) // If the player currently isn't mantling an object.
        {
            Debug.DrawRay(m_ChestPosition, transform.forward, Color.red);
            if (Physics.Raycast(m_ChestPosition, transform.forward, out hit, ChestRayLength, ~IgnoreCollisionMask, QueryTriggerInteraction.Ignore)) // If the raycast forward hits something
            {
                float DotProductPerc = Vector3.Dot(hit.normal, PlayerBrain.s_MasterBrainRef.p_PlayerRef.transform.forward); // Calculate the dot product between the hit normal and the forward of the player.
                // This is to check if the player isn't turned away from the surface that was hit.
                if (DotProductPerc < 0 && DotProductPerc < AngleLimitPercentage)
                {

                    Vector3 HitSurfaceNormal = hit.normal; // Store the hit normal
                    bool IsValidSurface = CheckIfValidSurface(HitSurfaceNormal); // Check if its a valid wall surface
                    if (IsValidSurface && hit.transform.CompareTag("Unclimable") == false) // Check if the surface returned true and that its climbable
                    {
                        IsLedgeAvailable = true;
                        m_wallHit = hit.point; // Set the wall hit

                        return true;
                    }
                }

            }
        }
       
        return false;
    }
    /*
*  Creators: Darien
*  
*Input: vector3 point, float total height
*  
*  Desc: find a top edge to grapple to and calculate the displacement.
*  
*  Output: Vector3
*  
*  Last Edit: 16 / 11 / 2018 (Darien, added function)
*/
    protected Vector3 FindGrapplePoint(Vector3 collidedPoint, float totalHeight)
    {
            RaycastHit hit;

            float HeightOffset = totalHeight; // Calculate the maximum height of our check based on our max mantle height and the height difference passed in.
            // The total height should be the the maximum height the player can grab.
        Debug.DrawRay(new Vector3(collidedPoint.x, collidedPoint.y + HeightOffset, collidedPoint.z), Vector3.down * HeightOffset, Color.green);
            if (Physics.BoxCast(new Vector3(collidedPoint.x, collidedPoint.y + HeightOffset, collidedPoint.z), SearchBoxSize, Vector3.down, out hit, Quaternion.Euler(Vector3.zero), HeightOffset, ~IgnoreCollisionMask, QueryTriggerInteraction.Ignore)) // If the boxcast hits
            {
                // A boxcast is shot downwards and checks if it collides with something.
               if (hit.normal == Vector3.up) // check if the normal is facing directly up.
               {
                  
                      MantleHitPoint = hit.point; // Store the mantle hit point
                      Vector3 MantlePos = CalculateEdgeOffset(m_wallHit, MantleHitPoint); // Calculate the offset relative to the point of contact.
                      //Debug.Log(hit.collider.name);
                      
                      return MantlePos;
                  
               }
                  
            }

   
        return Vector3.zero;
    }
    /*
 *  Creators: Darien
 *  
 *  
 *  Desc: It will draw when gizmo is enabled.
 *  
 *  Output: None
 *  
 *  Last Edit: 30 / 11 / 2018 (Darien, Added description)
 */
    private void OnDrawGizmos()
    {
        m_ChestPosition = transform.position + new Vector3(0, transform.lossyScale.y / 2f + RayHeightDifference, 0);
        Debug.DrawRay(m_ChestPosition, transform.forward * ChestRayLength, Color.red);

        Debug.DrawRay(m_ChestPosition, transform.up * MaxMantleHeight, Color.magenta);        
 

       if(m_wallHit != Vector3.zero)
        {
            Gizmos.DrawCube(new Vector3(m_wallHit.x, m_HeightOffset + m_wallHit.y, m_wallHit.z), SearchBoxSize);
        }


       if(MantleHitPoint != Vector3.zero)
        {
            Gizmos.color = new Color(1, 0, 0, 1.0f);
            Gizmos.DrawCube(new Vector3(MantleHitPoint.x, MantleHitPoint.y, MantleHitPoint.z), new Vector3( SearchBoxSize.x, MaxMantleHeight, SearchBoxSize.z));
        }
        


    }
    /*
*  Creators: Darien
*  
*   Input: bool condition
*  
*  Desc:  Set the grappling bool based on the condition
*  
*  

*  
*  Last Edit: 16 / 11 / 2018 (Darien, added function)
*/
    public void SetIsGrappling(bool condition)
    {
        IsGrappling = condition;
        IsLedgeAvailable = condition;


    }

    public void SetIsMantling(bool condition)
    {
        IsMantleAvailable = condition;
        IsLedgeAvailable = condition;
    }

    /*
*  Creators: Darien
*  
*Input: vector3 normal
*  
*  Desc: Checks if the normals are forward, back, left and right, basically determines if it isn't an angled surface.
*  
*  

*  
*  Last Edit: 16 / 11 / 2018 (Darien, added function)
*/
    bool CheckIfValidSurface(Vector3 normal)
    {
    
       // Compare the normals. If they slightly rotated return false
        if(normal.y > 0 || normal.y < 0)
        {
            
            return false;
        }
     
       
           
            return true;
        

       
    }
    /*
*  Creators: Darien
*  
*Input: Vector3 hitpoint, Vector3 Top of ledge
*  
*  Desc: Called when the player respawns, just clears the players velocity and resets the health.
*  
*  

*  
*  Last Edit: 16 / 11 / 2018 (Darien, added function)
*/
    Vector3 CalculateEdgeOffset(Vector3 hitpoint, Vector3 TopOfLedge)
    {
        

        
        
            Vector3 Point = new Vector3(hitpoint.x, TopOfLedge.y - transform.lossyScale.y / 2, hitpoint.z); // Store it in a point.

        Mathf.Clamp(Point.y, TopOfLedge.y - transform.lossyScale.y / 2, (hitpoint.y + MaxMantleHeight) - (transform.lossyScale.y / 2));

            return Point; // Return it
        
      
    }

    public bool GetIsGrabbing()
    {
        return IsGrappling;
    }



    Vector3 m_wallHit;

    bool m_CanGrapple = true;

    bool m_Recovering;

    float m_RecoveryTimer;

    private PlayerCreature m_PlayerRef;


    private Vector3 m_ChestPosition;


    private float m_HeightOffset;

    private ThirdPersonCamera m_ThirdPersonCamera;
}

==========================================================================
                            UNITY SETUP MANUAL
==========================================================================


  UNITY SETUP INSTRUCTIONS:
  
  1. Create a new GameObject and name it "Player".
  2. Attach a Rigidbody component to the Player and enable Interpolate.
  3. Create a child GameObject of Player and name it "CameraHolder". Attach your Main Camera to this.
  4. Attach this SnapDragon script to the Player.
  5. Assign the CameraHolder Transform to the "cameraTransform" field.
  6. Assign the Main Camera to the "playerCamera" field.
  7. Set the LayerMask fields "groundLayer" and "wallLayer" appropriately in the inspector.
  8. Tweak movement values (speed, jumpForce, etc.) to your liking.
  
  - CONTROLS:
  - W/A/S/D: Move
  - Left Shift: Sprint
  - Space: Jump
  - Wallrunning is automatic when jumping toward a wall
  - Left Control: Slide
  
  This script simulates Titanfall-style momentum-based movement.
 
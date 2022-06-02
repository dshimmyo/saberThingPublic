using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBurnable 
{
    void Burn(RaycastHit hit);//Vector2 as uv or Vector3 as worldspace position? or a raycast hit?
    void Burn(RaycastHit[] hits);//multiple hits
    void Burn(RaycastHit hit, RaycastHit previousHit);//Vector2 as uv or Vector3 as worldspace position? or a raycast hit?

}

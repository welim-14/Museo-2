using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class Compass : MonoBehaviour
{
    public RawImage CompassImage;
    public Transform Player;
    public Text CompassDirectionText;

    // Update is called once per frame
    void Update()
    {
        // Obtener el manejo de las imagenes uvRect
        CompassImage.uvRect = new Rect(Player.localEulerAngles.y / 360, 0, 1, 1);

        // Obtener una copia de del vector de movimiento hacia adelante
        Vector3 forward = Player.transform.forward;

        // zero out  el componente y del vector de movimiento en el plano x,z 
        forward.y = 0;

        //ajustar los angulos para que estos se den en incrementos de 5 grados
        float headingAngle = Quaternion.LookRotation(forward).eulerAngles.y;
        headingAngle = 1 * (Mathf.RoundToInt(headingAngle));

        //convertir flotantes a entero
        int diaplayangle;
        diaplayangle = Mathf.RoundToInt(headingAngle);

        // Ajustar el texto de la brujula a la imagen de texto de la brujula 
        switch (diaplayangle)
        {
                case 0:
                // haz lo siguiente
                CompassDirectionText.text = "N";
                break;
                case 360:
                // haz lo siguiente
                CompassDirectionText.text = "N";
                break;
                case 45:
                // haz lo siguiente
                CompassDirectionText.text = "NE";
                break;
                case 90:
                // haz lo siguiente
                CompassDirectionText.text = "E";
                break;
                case 130:
                // haz lo siguiente
                CompassDirectionText.text = "SE";
                break;
                case 180:
                // haz lo siguiente
                CompassDirectionText.text = "S";
                break;
                case 225:
                // haz lo siguiente
                CompassDirectionText.text = "SW";
                break;
                case 270:
                // haz lo siguiente
                CompassDirectionText.text = "W";
                break;
                default:
                CompassDirectionText.text = headingAngle.ToString();
                break;
        }
        
    }
}
Asset Name: Mesh Normals Debugger / Reverser
Version: 1.1
Author: QOOBIT Productions Inc.
Author URL: https://qoobit.com
Support E-mail: info@qoobit.com
Description: 

WHAT'S NEW
===========
v1.1		- No Longer required to use play mode to visualize normals
			- Shortened and aggregated scripts for better performance
			- More intuitive GUI Inspector controls

v1.0.0		- Original Source
-------------

This tool is used to visualize and debug normal information on meshes inside Unity 3D. 
By adding the "DisplayGameObjectNormals" component to a GameObject, all child "Mesh Filter" 
and "Skinned Mesh Renderer" will have their mesh normals displayed in the Scene window.

By default vertex normals are displayed and you have the option to adjust normal colors on a 
GameObject basis or on individual mesh objects. You can also choose to display face normals.
There are two options for displaying face normals: Orthogonal or Averaged Vertex Normals.

The components "DisplayMeshFilterNormals" and "DisplaySkinnedMeshNormals" are meant to act on a 
single GameObject with a "Mesh Filter" or "Skinned Mesh Renderer" component.

If you wish to display all normals across a GameObject, simply add the "DisplayGameObjectNormals" 
once to the parent GameObject. This script will traverse the entire tree and add 
"DisplayMeshFilterNormals" and "DisplaySkinnedMeshNormals" to any child GameObjects that have a 
"Mesh Filter" or "Skinned Mesh Renderer" component.

The control hieraracy is set up in a top down method. Adjusting the settings in the 
"DisplayGameObjectNormals" component will cascade down to any child GameObject with identical 
settings. Any child components with differently tweaked settings, will override the settings on 
the highest level. If you wish to reset an edited child GameObject's component settings to settings
on the highest level, click the "Constrain Normals to Parent GameObject Settings" button in the 
"DisplayMeshFilterNormals" or "DisplaySkinnedMeshNormals" component.

Please note that the "Constrain Normals to Parent GameObject Settings" is only used if there is
a parent GameObject to constrain to.

NOTE: Be sure to use the "Destroy Component" button on the "Display Game Object Normals" component 
to properly clean up all normals in the hierarchy. If you forgot to do so, just add another 
"Display Game Object Normals" component to a GameObject and press the button to clean up all the 
normals.

*** Bonus Feature ***

By checking the "Reversed" check box in the component, the normals can be flipped. 
This is useful for backface culling, or to fake two-sided polys.


Potential Future Upgrade(s):
Depending on the community's feedback, we may consider adding new features to this tool.

*** SPECIAL THANKS ***

Asset URL : https://www.assetstore.unity3d.com/en/#!/content/20031
YouTube Clip Music : http://freemusicarchive.org/music/Starpause/staRpauSe_singLes/Hooky_Hicky_bY_staRpauSe_1993

//    MIT License
//    
//    Copyright (c) 2017 Dustin Whirle
//    
//    My Youtube stuff: https://www.youtube.com/playlist?list=PL-sp8pM7xzbVls1NovXqwgfBQiwhTA_Ya
//    
//    Permission is hereby granted, free of charge, to any person obtaining a copy
//    of this software and associated documentation files (the "Software"), to deal
//    in the Software without restriction, including without limitation the rights
//    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//    copies of the Software, and to permit persons to whom the Software is
//    furnished to do so, subject to the following conditions:
//    
//    The above copyright notice and this permission notice shall be included in all
//    copies or substantial portions of the Software.
//    
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//    SOFTWARE.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BLINDED_AM_ME{

	[ExecuteInEditMode]
	[RequireComponent(typeof(ParticleSystem))]
	[RequireComponent(typeof(Path_Comp))]
	public class ParticlePathFlow : MonoBehaviour {

		public class PathParticleTracker
		{
			public ParticleSystem.Particle particle;
			public float distance;
			public float rotation;

			public PathParticleTracker()
			{

				this.particle = new ParticleSystem.Particle();
				this.particle.remainingLifetime = 0.0f;

			}

			public void Revive(ParticleSystem systemRef){

				this.distance = Random.Range(0.0f, 1.0f);
				this.rotation = Random.Range(0.0f, 360.0f);
				
				this.particle.startLifetime = systemRef.main.startLifetime.constant;
				this.particle.remainingLifetime = this.particle.startLifetime;
				this.particle.startColor = systemRef.main.startColor.color;
				this.particle.startSize = systemRef.main.startSize.constant;
				this.particle.rotation = systemRef.main.startRotation.constant;
			}
		}

		public float emissionRate = 25.0f;
		private float _emissionRateTracker = 0.0f;


		[Range(0.0f, 5.0f)]
		public float pathWidth = 0.0f;

		private int                       _particle_count;
		private PathParticleTracker[]     _particle_trackerArray;
		private ParticleSystem.Particle[] _particle_array;
		private ParticleSystem            _particle_system;


		private double _editorTimeDelta = 0.0f;
		private double _editorTimetracker = 0.0f;


		private Path_Comp _path_comp;

		void OnEnable () {

			_path_comp = GetComponent<Path_Comp>();

			_particle_system = GetComponent<ParticleSystem>();
			ParticleSystem.EmissionModule em = _particle_system.emission;
			em.enabled = false;

			_particle_array        = new ParticleSystem.Particle[_particle_system.main.maxParticles];

			_particle_trackerArray = new PathParticleTracker[_particle_system.main.maxParticles];
			for(int i=0; i<_particle_trackerArray.Length; i++)
				_particle_trackerArray[i] = new PathParticleTracker();

			if(_particle_system.main.prewarm){
				float numRevive = Mathf.Floor(emissionRate * _particle_system.main.startLifetime.constant);
				for(int i=0; i<numRevive; i++){
					_particle_trackerArray[i].Revive(_particle_system);
					_particle_trackerArray[i].particle.remainingLifetime = _particle_trackerArray[i].particle.startLifetime * ((float)i/numRevive);
				}
			}

			_emissionRateTracker = 1.0f/emissionRate;


	#if UNITY_EDITOR
			if(!Application.isPlaying){
				_editorTimetracker = EditorApplication.timeSinceStartup;
			}
	#endif

		}

		void LateUpdate () {

	#if UNITY_EDITOR
			if(!Application.isPlaying){
				_editorTimeDelta = EditorApplication.timeSinceStartup - _editorTimetracker;
				_editorTimetracker = EditorApplication.timeSinceStartup;

				ParticleSystem.EmissionModule em = _particle_system.emission;
				em.enabled = false;
			}
	#endif

			if(transform.childCount <= 1)
				return;

			// emision
			if(_emissionRateTracker <= 0.0f){
				_emissionRateTracker += 1.0f/emissionRate;

				RenewOneDeadParticle();
			}
			_emissionRateTracker -= (Application.isPlaying ? Time.deltaTime : (float) _editorTimeDelta);

			// age them
			foreach(PathParticleTracker tracker in _particle_trackerArray)
			if(tracker.particle.remainingLifetime > 0.0f){
				tracker.particle.remainingLifetime = Mathf.Max(tracker.particle.remainingLifetime - (Application.isPlaying ? Time.deltaTime : (float) _editorTimeDelta), 0.0f);
			}


			float normLifetime = 0.0f;
			Path_Point Rpoint;

			// move them
			foreach(PathParticleTracker tracker in _particle_trackerArray)
			if(tracker.particle.remainingLifetime > 0.0f){

				normLifetime = tracker.particle.remainingLifetime/tracker.particle.startLifetime;
				normLifetime = 1.0f - normLifetime;
				
				Rpoint = _path_comp.GetPathPoint(normLifetime * _path_comp.TotalDistance);

				// rotate around Rpoint.direction
			
				Rpoint.point += (pathWidth * tracker.distance) * Math_Functions.Rotate_Vector(Rpoint.up, Rpoint.forward, tracker.rotation);

				tracker.particle.position = Rpoint.point;
				tracker.particle.velocity = Rpoint.forward;
			
			}

			_particle_count = 0;

			// set the given array
			foreach(PathParticleTracker tracker in _particle_trackerArray)
			if(tracker.particle.remainingLifetime > 0.0f){ // it's alive
				_particle_array[_particle_count] = tracker.particle;
				_particle_count++;
			}
			
			_particle_system.SetParticles(_particle_array, _particle_count);

		}

		void RenewOneDeadParticle(){

			for(int i=0; i<_particle_trackerArray.Length; i++)
			if(_particle_trackerArray[i].particle.remainingLifetime <= 0.0f){
				_particle_trackerArray[i].Revive(_particle_system);
				break;
			}
		}
			
	}
}
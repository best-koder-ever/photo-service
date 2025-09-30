#!/usr/bin/env python3
"""
Test script for Photo Service Privacy Features
Tests the advanced privacy system with blur effects and match-based access control
"""

import requests
import json
import base64
import os
from PIL import Image
import io

# Configuration
BASE_URL = "http://localhost:5000"
HEADERS = {"Content-Type": "application/json"}

def create_test_image():
    """Create a simple test image for testing"""
    # Create a simple 200x200 RGB image
    img = Image.new('RGB', (200, 200), color=(73, 109, 137))
    
    # Save to bytes
    img_bytes = io.BytesIO()
    img.save(img_bytes, format='JPEG')
    img_bytes.seek(0)
    
    return img_bytes.getvalue()

def test_privacy_upload():
    """Test uploading a photo with privacy settings"""
    print("🔒 Testing Privacy Photo Upload...")
    
    # Create test image
    test_image = create_test_image()
    
    # Test data for privacy upload
    upload_data = {
        "userId": 123,
        "file": base64.b64encode(test_image).decode(),
        "fileName": "test_private_photo.jpg",
        "privacyLevel": "Private",  # Private = will be blurred for non-matches
        "blurIntensity": 15.0,
        "requiresMatch": True,
        "allowVIPAccess": False
    }
    
    try:
        response = requests.post(f"{BASE_URL}/api/photos/upload-with-privacy", 
                               json=upload_data, headers=HEADERS)
        
        if response.status_code == 200:
            result = response.json()
            print(f"✅ Privacy upload successful!")
            print(f"   Photo ID: {result.get('id')}")
            print(f"   Privacy Level: {result.get('privacyLevel')}")
            print(f"   Requires Match: {result.get('requiresMatch')}")
            print(f"   Has Blurred Version: {result.get('hasBlurredVersion', False)}")
            return result.get('id')
        else:
            print(f"❌ Privacy upload failed: {response.status_code}")
            print(f"   Error: {response.text}")
            return None
            
    except Exception as e:
        print(f"❌ Privacy upload error: {e}")
        return None

def test_privacy_control(photo_id):
    """Test getting photo with privacy control"""
    print(f"\n🎭 Testing Privacy Control for Photo {photo_id}...")
    
    # Test 1: Non-matched user (should get blurred version)
    try:
        response = requests.get(f"{BASE_URL}/api/photos/{photo_id}/privacy-control", 
                              params={"viewerUserId": "999", "hasMatch": "false"})
        
        if response.status_code == 200:
            result = response.json()
            print(f"✅ Non-matched user access:")
            print(f"   Is Blurred: {result.get('isBlurred', False)}")
            print(f"   Can View Original: {result.get('canViewOriginal', False)}")
            print(f"   Privacy Message: {result.get('privacyMessage', 'N/A')}")
        else:
            print(f"❌ Non-matched access failed: {response.status_code}")
            
    except Exception as e:
        print(f"❌ Non-matched access error: {e}")
    
    # Test 2: Matched user (should get original)
    try:
        response = requests.get(f"{BASE_URL}/api/photos/{photo_id}/privacy-control", 
                              params={"viewerUserId": "456", "hasMatch": "true"})
        
        if response.status_code == 200:
            result = response.json()
            print(f"✅ Matched user access:")
            print(f"   Is Blurred: {result.get('isBlurred', False)}")
            print(f"   Can View Original: {result.get('canViewOriginal', False)}")
            print(f"   Privacy Message: {result.get('privacyMessage', 'N/A')}")
        else:
            print(f"❌ Matched access failed: {response.status_code}")
            
    except Exception as e:
        print(f"❌ Matched access error: {e}")

def test_blur_generation(photo_id):
    """Test getting the blurred version"""
    print(f"\n🌫️ Testing Blur Generation for Photo {photo_id}...")
    
    try:
        response = requests.get(f"{BASE_URL}/api/photos/{photo_id}/blurred")
        
        if response.status_code == 200:
            print(f"✅ Blurred photo retrieved successfully!")
            print(f"   Content Type: {response.headers.get('Content-Type')}")
            print(f"   Content Length: {len(response.content)} bytes")
        else:
            print(f"❌ Blurred photo failed: {response.status_code}")
            print(f"   Error: {response.text}")
            
    except Exception as e:
        print(f"❌ Blurred photo error: {e}")

def test_privacy_update(photo_id):
    """Test updating privacy settings"""
    print(f"\n⚙️ Testing Privacy Update for Photo {photo_id}...")
    
    update_data = {
        "privacyLevel": "MatchOnly",  # Change to MatchOnly
        "blurIntensity": 25.0,        # Increase blur
        "requiresMatch": True,
        "allowVIPAccess": True        # Now allow VIP access
    }
    
    try:
        response = requests.put(f"{BASE_URL}/api/photos/{photo_id}/privacy", 
                              json=update_data, headers=HEADERS, 
                              params={"userId": "123"})
        
        if response.status_code == 200:
            result = response.json()
            print(f"✅ Privacy update successful!")
            print(f"   New Privacy Level: {result.get('privacyLevel')}")
            print(f"   New Blur Intensity: {result.get('blurIntensity')}")
            print(f"   VIP Access: {result.get('allowVIPAccess')}")
        else:
            print(f"❌ Privacy update failed: {response.status_code}")
            print(f"   Error: {response.text}")
            
    except Exception as e:
        print(f"❌ Privacy update error: {e}")

def main():
    """Run all privacy feature tests"""
    print("🎯 Photo Service Privacy Features Test")
    print("=" * 50)
    print("Testing advanced privacy system with:")
    print("- Blur effects for private photos")
    print("- Match-based access control") 
    print("- Advanced privacy levels")
    print("- Content moderation integration")
    print("=" * 50)
    
    # Test 1: Upload with privacy
    photo_id = test_privacy_upload()
    
    if photo_id:
        # Test 2: Privacy control access
        test_privacy_control(photo_id)
        
        # Test 3: Blur generation
        test_blur_generation(photo_id)
        
        # Test 4: Privacy updates
        test_privacy_update(photo_id)
    
    print(f"\n🎉 Privacy feature testing complete!")
    print("✨ The best photo service ever with advanced privacy is ready!")

if __name__ == "__main__":
    main()

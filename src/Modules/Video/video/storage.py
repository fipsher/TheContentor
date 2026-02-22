import os
import shutil
import uuid

from video.config import STORAGE_BASE_PATH


def save_to_local_storage(file_path, container_name):
    """Save file to local storage, return (containerName, assetPath)"""
    name, ext = os.path.splitext(os.path.basename(file_path))
    unique_name = f"{name}-{uuid.uuid4()}{ext}"
    container_dir = os.path.join(STORAGE_BASE_PATH, container_name)
    os.makedirs(container_dir, exist_ok=True)
    shutil.copy2(file_path, os.path.join(container_dir, unique_name))
    return container_name, unique_name

def read_from_local_storage(container_name, asset_path, output_path):
    """Copy file from local storage to output_path"""
    src = os.path.join(STORAGE_BASE_PATH, container_name, asset_path)
    if not os.path.exists(src):
        raise FileNotFoundError(f"Blob not found: {container_name}/{asset_path}")
    shutil.copy2(src, output_path)

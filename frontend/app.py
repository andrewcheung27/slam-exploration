import os
from flask import Flask, render_template, request, redirect, url_for, send_from_directory
import cv2
import numpy as np
from werkzeug.utils import secure_filename
import subprocess
import shutil

app = Flask(__name__)
app.config['UPLOAD_FOLDER'] = '/code/jjiang23/SLAMPROJ/slam-exploration/frontend/uploads'
app.config['PROCESSED_FOLDER'] = '/code/jjiang23/SLAMPROJ/slam-exploration/frontend/processed'
app.config['ALLOWED_EXTENSIONS'] = {'mp4', 'avi', 'mov', 'wmv', 'mkv'}
app.config['PYTHON'] = "/home/jjiang23/miniconda3/envs/mast3r-slam/bin/python"
app.config["PROGRAM"] = "/code/jjiang23/SLAMPROJ/MASt3R-SLAM/main.py"
app.config["CONFIG"] = "/code/jjiang23/SLAMPROJ/MASt3R-SLAM/config/base.yaml"

# Create directories if they don't exist
os.makedirs(app.config['UPLOAD_FOLDER'], exist_ok=True)
os.makedirs(app.config['PROCESSED_FOLDER'], exist_ok=True)

def allowed_file(filename):
    return '.' in filename and \
           filename.rsplit('.', 1)[1].lower() in app.config['ALLOWED_EXTENSIONS']

def process_video(input_path, output_path, processed_folder):
    print("processing video")
    working_dir = "/code/jjiang23/SLAMPROJ/MASt3R-SLAM"
    
    subprocess.run([app.config['PYTHON'],
                  app.config["PROGRAM"],
                  '--dataset', input_path, '--config',
                  app.config["CONFIG"]], cwd=working_dir)
    
    print("finished video")
    
    if os.path.exists(output_path):
        print("File exists!")
        # Copy the processed file to our processed folder for serving to the user
        filename = os.path.basename(output_path)
        destination = os.path.join(processed_folder, filename)
        shutil.copy2(output_path, destination)
        return True
    else:
        print("File does not exist.")
        return False

@app.route('/')
def index():
    return render_template('index.html')

@app.route('/upload', methods=['POST'])
def upload_file():
    if 'file' not in request.files:
        return redirect(request.url)
    
    file = request.files['file']
    if file.filename == '':
        return redirect(request.url)
    
    if file and allowed_file(file.filename):
        filename = secure_filename(file.filename)
        file_path = os.path.join(app.config['UPLOAD_FOLDER'], filename)
        file.save(file_path)
        
        # Process the video
        base_filename = filename.rsplit('.', 1)[0]
        output_path = os.path.join("/code/jjiang23/SLAMPROJ/MASt3R-SLAM/logs", base_filename + '.ply')
        print(output_path)
        
        success = process_video(file_path, output_path, app.config['PROCESSED_FOLDER'])
        
        if success:
            print("video uploaded and processed!")
            return redirect(url_for('show_result', filename=base_filename + '.ply'))
        else:
            return "Error processing video", 500
    
    return "Invalid file type", 400

@app.route('/results/<filename>')
def show_result(filename):
    return render_template('result.html', filename=filename)

@app.route('/download/<filename>')
def download_file(filename):
    return send_from_directory(app.config['PROCESSED_FOLDER'], filename, as_attachment=True)

if __name__ == '__main__':
    app.run(debug=True)
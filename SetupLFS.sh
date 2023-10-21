echo.
	Echo Configuring LFS
	git lfs install

echo.
	echo IMAGES
	git lfs track "*.psd"
	git lfs track "*.png"
	git lfs track "*.bmp"
	git lfs track "*.gif"
	git lfs track "*.jpeg"
	git lfs track "*.jpg"

	echo.
	echo AUDIO
	git lfs track "*.aif"
	git lfs track "*.wav"
	git lfs track "*.mp3"
	git lfs track "*.ogg"

	echo.
	echo 3D
	git lfs track "*.fbx"
	git lfs track "*.dae"
	git lfs track "*.3ds"
	git lfs track "*.dxf"
	git lfs track "*.obj"
	git lfs track "*.skp"
	git lfs track "*.ma"
	git lfs track "*.mb"
	git lfs track "*.max"
	git lfs track "*.c4d"
	git lfs track "*.blend"
	git lfs track "*.shadergraph"

	echo.
	echo SPEED TREE
	git lfs track "*.spt"
	git lfs track "*.tga"

	echo.
	echo VIDEO
	git lfs track "*.webm"
	git lfs track "*.mkv"
	git lfs track "*.vob"
	git lfs track "*.ogv"
	git lfs track "*.drc"
	git lfs track "*.gifv"
	git lfs track "*.mng"
	git lfs track "*.avi"
	git lfs track "*.mov"
	git lfs track "*.qt"
	git lfs track "*.wmv"
	git lfs track "*.yuv"
	git lfs track "*.rm"
	git lfs track "*.rmvb"
	git lfs track "*.asf"
	git lfs track "*.amv"
	git lfs track "*.m4p"
	git lfs track "*.svi"
	git lfs track "*.3gp"
	git lfs track "*.3g2"
	git lfs track "*.mxf"
	git lfs track "*.roq"
	git lfs track "*.nsv"
	git lfs track "*.flv"
	git lfs track "*.f4v"
	git lfs track "*.f4p"
	git lfs track "*.f4a"
	git lfs track "*.f4b"
	git lfs track "*.mp4"
	
	echo.
	echo BINARY
	git lfs track "*.bin"

	echo.
	echo SIGNED DISTANCE FIELDS
	git lfs track .sdf

	echo.
	echo ASSET BUNDLE
	git lfs track "*.manifest"
	git lfs track "*.assetbundle"
	
	echo.
	echo PLUGINS
	git lfs track "*.dll"
	git lfs track "*.so"
	git lfs track "*.lib"
	
	echo.
	echo ==================================
	echo.

	git add .gitattributes
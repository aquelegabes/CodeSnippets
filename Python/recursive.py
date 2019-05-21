import os

def read_files(path):
	"""
	Returns all filenames on specified folder and its subfolders
	"""
	subpastas = list(filter(lambda x: os.path.isdir(os.path.join(path,x)), os.listdir(path)))
	if subpastas:
		for pasta in subpastas:
			read_files(os.path.join(path,pasta))
	print (path)
	print (list(filter(lambda x: not os.path.isdir(os.path.join(path,x)), os.listdir(path))))

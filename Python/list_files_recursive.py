import os

def listar_pasta(path):
	if os.path.isdir(path):
		subpastas = list(filter(lambda x: os.path.isdir(os.path.join(path,x)), os.listdir()))
		for pasta in subpastas:
			listar_pasta(os.path.join(path,pasta))
	else:
		print (list(filter(lambda x: not os.path.isdir(os.path.join(path,x)),os.listdir())))


listar_pasta('C:\\Users\\gabriel.santana\\Documents\\Gabriel Santos\\etc\\prova')

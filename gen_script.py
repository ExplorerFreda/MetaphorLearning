# -*- coding: utf-8 -*-
"""
Created on Mon Jul 04 14:54:23 2016

@author: v-zw
"""

def main():
    repeat = 64

    f = open("d:/v-zw/metaphor_learning/modules/auto_scripts.txt", 'w')
    
    for i in range(repeat):
        line = "SELECT * FROM Corpus" + str(i) + "\nUNION ALL"
        f.write(line)
        f.write('\n')
        
    f.close()
    
if __name__=="__main__":
    main()
        
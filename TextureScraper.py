# import scrapy
from selenium import webdriver
from selenium.webdriver.common.keys import Keys
import pandas as pd
import subprocess
from time import sleep
import random
import tqdm

def SleepRandom(duration):
    factor = random.randrange(80, 140)/99.0
    sleep((factor**2)*duration)
    
subprocess.call("ls Textures".split())#check

driver = webdriver.Chrome('/Users/MyUsername/Downloads/chromedriver')
next_url = "https://minecraft.fandom.com/wiki/Category:Block_textures?fileuntil=Cherry+Planks+%28texture%29+JE1.png#mw-category-media"
image_url_map = dict()
while(next_url):
    driver.get(next_url)

    galleryboxes = driver.find_elements_by_class_name("gallerybox")
    
    for gallerybox in tqdm(galleryboxes):
        image_url = gallerybox.find_element_by_class_name("image").get_attribute("href")
        image_name = gallerybox.find_element_by_class_name("image").get_attribute("data-image-name")
        SleepRandom(1.2)
    print(f"adding image url: {image_url}")
    image_url_map[image_name] = image_url
    
    try:
        next_url = driver.find_elements_by_xpath('next page"]') #//a[@title="Category:Block textures"]
    except Exception as e:
        print(e)
        next_url = ""
    if not next_url:
        break
    
driver.close()
print(image_url_map)

for image_name, image_url in image_url_map.items():
    subprocess.call(f"wget -o ./Textures {image_url}".split())
with open("image_url_map.txt") as f:
    f.write(image_url_map)

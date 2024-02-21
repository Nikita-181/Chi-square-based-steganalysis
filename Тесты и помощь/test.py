from PIL import Image
from scipy import stats

def chi_squared_test(channel):
    """Main function for the attack

    Using chi-squared implementation
    from scipy.

    :param channel: Channel for analyzing 

    """
    hist = calc_colors(channel)

    expected_freq, observed_freq = calc_freq(hist)

    chis, probs = stats.chisquare(observed_freq, expected_freq)
      
    return chis, probs

def calc_colors(channel):
    """Prepare color histogram for further calculations"""
    hist = channel.histogram()
    hist = list(map(lambda x: 1 if x == 0 else x, hist)) # to avoid dividing by zero 
    return hist

def calc_freq(histogram):
    """Calculating expacted and observed freqs"""
    expected = []
    observed = []
    test = len(histogram) // 2
    for k in range(0, len(histogram) // 2):
        expected.append((histogram[2 * k] + histogram[2 * k + 1]) / 2)
        observed.append(histogram[2 * k])

    return expected, observed



img = Image.open(r"C:\Users\Userr\Desktop\Стеганография\Chi-square based steganalysis\LSB.png")

channels = img.split()
width, height = img.size
for i in range(height):
    prob = 0
    for ch in channels:
        data = ch.crop((0, i, width, i+1)) # crop for new line 
        prob += chi_squared_test(data)[1]

print("end")

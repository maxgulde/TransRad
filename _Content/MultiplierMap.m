% Constructing a multiplier map for the hemicube radiosity approach
% Author: Max Gulde
% Last Update: 2018-05-31

clear
clc

% Parameters
p_HemiCubeSize = 100;

% Secondary
p_MapSize = p_HemiCubeSize * 2;
p_CutOutSize = p_HemiCubeSize / 2;
p_Center = [p_HemiCubeSize p_HemiCubeSize] + 0.5;

%% Create Lambertian mask
Lambert = zeros(p_MapSize, p_MapSize);
for ii = 1:p_MapSize * p_MapSize
    xx = mod(ii,p_MapSize);
    yy = floor(ii/p_MapSize);
    if (xx < p_CutOutSize && yy < p_CutOutSize)
        continue;
    end
    if (xx < p_CutOutSize && yy > p_CutOutSize + p_HemiCubeSize)
        continue;
    end
    if (xx > p_CutOutSize + p_HemiCubeSize && yy < p_CutOutSize)
        continue;
    end
    if (xx > p_CutOutSize + p_HemiCubeSize && yy > p_CutOutSize + p_HemiCubeSize)
        continue;
    end        
    % Distance to center
    ToCenter = abs(p_Center - [xx yy]);
    AngleC = norm(ToCenter) / p_HemiCubeSize  * pi/2;
    if (AngleC > pi/2)
        continue;
    end
    Lambert(xx,yy) = cos(AngleC);
end

figure(1);
imagesc(Lambert);
axis equal;
colormap gray;
colorbar

%% Create Shape compensation mask
Shape = zeros(p_MapSize, p_MapSize);
for ii = 1:p_MapSize * p_MapSize
    xx = mod(ii,p_MapSize) + 1;
    yy = floor(ii/p_MapSize) + 1;
    if (xx < p_CutOutSize && yy < p_CutOutSize)
        continue;
    end
    if (xx < p_CutOutSize && yy > p_CutOutSize + p_HemiCubeSize)
        continue;
    end
    if (xx > p_CutOutSize + p_HemiCubeSize && yy < p_CutOutSize)
        continue;
    end
    if (xx > p_CutOutSize + p_HemiCubeSize && yy > p_CutOutSize + p_HemiCubeSize)
        continue;
    end        
    % Distances
    ToCenter = (p_Center - [xx yy]);
    ToBorderL = ([0 p_HemiCubeSize] - [xx yy]);
    ToBorderR = ([p_MapSize p_HemiCubeSize] - [xx yy]);
    ToBorderU = ([p_HemiCubeSize 0] - [xx yy]);
    ToBorderD = ([p_HemiCubeSize p_MapSize] - [xx yy]);
    % Angles
    AngleC = norm(ToCenter) / p_HemiCubeSize * pi/2;
    AngleL = norm(ToBorderL) / p_HemiCubeSize * pi/2;
    AngleR = norm(ToBorderR) / p_HemiCubeSize * pi/2;
    AngleU = norm(ToBorderU) / p_HemiCubeSize * pi/2;
    AngleD = norm(ToBorderD) / p_HemiCubeSize * pi/2;
    Angle = min([AngleC AngleL AngleR AngleU AngleD]);
    if (Angle > pi/2)
        continue;
    end
    Shape(xx,yy) = cos(Angle);
end

figure(2);
imagesc(Shape);
axis equal;
colormap gray;
colorbar

%% Multiply and normalize the two maps

Map = Shape .* Lambert;
N = sum(sum(Map));
Map = Map / N;

figure(3);
imagesc(Map);
axis equal;
colormap gray;
colorbar


% %% Check
% Line1 = Lambert(100,:);
% Line2 = Lambert(:,100);
% x = 1:p_MapSize;
% y = cos(x/p_HemiCubeSize * pi/2 - pi/2);
% 
% figure(3);
% plot(Line1,'bx');
% hold on;
% plot(Line2,'g.');
% plot(x,y,'r-');
% hold off
